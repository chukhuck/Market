using FaG.Common;
using FaG.Data.DAL;
using FaG.Data.IndexModel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using TPulse.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddHttpClient("FaGClient");
builder.Services.AddSingleton(new TPulseApiClient(
    "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed?appName=invest&origin=web&platform=web&include=all",
    "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/post/instrument/{tiker}?appName=invest&origin=web&platform=web&include=all",
    "https://pulse-image-post.cdn-tinkoff.ru/{guid_image}-small.jpeg"
));

var fagConn = Environment.GetEnvironmentVariable("FAG_DB") ??
    "Host=localhost;Port=5432;Database=fagdb;Username=faguser;Password=fagpassword";

builder.Services.AddDbContext<FaGDbContext>(options => options.UseNpgsql(fagConn));

// Register available downloaders
builder.Services.AddTransient<IFagDownloader, TPulseDownloader>();

builder.Services.AddHostedService<ScheduledWorker>();

var app = builder.Build();

// Применение миграций БД
using (var scope = app.Services.CreateScope())
{
  var context = scope.ServiceProvider.GetRequiredService<FaGDbContext>();
  try
  {
    context.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully.");
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error applying database migrations: {ex.Message}");
    throw;
  }
}

app.MapGet("/", () => Results.Ok("FaG.Scheduler Web API is running"));

app.MapGet("/process-range", async (HttpContext ctx) =>
{
  var q = ctx.Request.Query;
  if (!q.TryGetValue("start", out var startVals) || !q.TryGetValue("end", out var endVals))
    return Results.BadRequest("Требуются параметры 'start' и 'end'");

  if (!TryParseDate(startVals[0], out var start) || !TryParseDate(endVals[0], out var end))
    return Results.BadRequest("Невозможно распарсить даты. Используйте формат 2023-01-01T00:00:00Z.");

  try
  {
    await Program.ProcessPostsByRangeAsync(ctx.RequestServices, start, end, ctx.RequestAborted).ConfigureAwait(false);
    return Results.Ok(new { started = start, ended = end });
  }
  catch (OperationCanceledException)
  {
    return Results.StatusCode((int)HttpStatusCode.RequestTimeout);
  }
  catch (Exception ex)
  {
    return Results.Problem(ex.Message);
  }
});

app.Run();

static bool TryParseDate(string? s, out DateTime dt)
{
  if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt))
    return true;

  if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
  {
    dt = DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();
    return true;
  }

  dt = default;
  return false;
}

public partial class Program
{
  public static async Task ProcessPostsByRangeAsync(
    IServiceProvider services,
    DateTime startUtc,
    DateTime endUtc,
    CancellationToken token)
  {
    using var scope = services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<FaGDbContext>();
    var downloaders = scope.ServiceProvider.GetServices<IFagDownloader>().ToList();

    if (downloaders.Count == 0)
      return;
    List<UserPostEvaluation> allPosts = await DownloadAllPostsAsync(
                                                                      startUtc,
                                                                      endUtc,
                                                                      downloaders,
                                                                      token)
                                              .ConfigureAwait(false);

    Dictionary<Guid, UserPostEvaluation> byId = ClearPosts(allPosts);
    await EvaluateAndSavePostAsync(db, byId, token).ConfigureAwait(false);
    await CalculateAndSaveFearAndGreadIndexAsync(startUtc, endUtc, db, token).ConfigureAwait(false);
  }
  private static async Task CalculateAndSaveFearAndGreadIndexAsync(
    DateTime startUtc,
    DateTime endUtc,
    FaGDbContext db,
    CancellationToken token)
  {
    try
    {
      var model = new SimpleIndexModel();

      var date = startUtc.Date;

      while (date < endUtc.Date)
      {
        var from = date;
        var to = date.AddDays(1);

        var total = await db.UserPostEvaluations
          .CountAsync(x => x.PostDate >= from && x.PostDate < to, token)
          .ConfigureAwait(false);

        var positive = await db.UserPostEvaluations
          .CountAsync(x => x.PostDate >= from && x.PostDate < to && x.Emotion == FaG.Data.DAL.Emotion.Positive, token)
          .ConfigureAwait(false);

        var negative = await db.UserPostEvaluations
          .CountAsync(x => x.PostDate >= from && x.PostDate < to && x.Emotion == FaG.Data.DAL.Emotion.Negative, token)
          .ConfigureAwait(false);

        var neutral = await db.UserPostEvaluations
          .CountAsync(x => x.PostDate >= from && x.PostDate < to && x.Emotion == FaG.Data.DAL.Emotion.Neutral, token)
          .ConfigureAwait(false);

        var unrated = await db.UserPostEvaluations
          .CountAsync(x => x.PostDate >= from && x.PostDate < to && x.Emotion == FaG.Data.DAL.Emotion.None, token)
          .ConfigureAwait(false);

        var scoreInt = model.ComputeScoreInt(positive, negative, neutral);
        var effective = total - unrated;
        var scoreNorm = model.Normalize(scoreInt, effective);

        var existing = await db.FearGreedIndices
          .FirstOrDefaultAsync(i => i.DateUtc == date && i.ModelName == model.Name, token)
          .ConfigureAwait(false);

        if (existing == null)
        {
          var rec = new FaG.Data.DAL.FearGreedIndex
          {
            DateUtc = date,
            ScoreInt = scoreInt,
            ScoreNormalized = scoreNorm,
            TotalPosts = total,
            PositivePosts = positive,
            NegativePosts = negative,
            NeutralPosts = neutral,
            UnratedPosts = unrated,
            ModelName = model.Name
          };
          await db.FearGreedIndices.AddAsync(rec, token).ConfigureAwait(false);
        }
        else
        {
          existing.ScoreInt = scoreInt;
          existing.ScoreNormalized = scoreNorm;
          existing.TotalPosts = total;
          existing.PositivePosts = positive;
          existing.NegativePosts = negative;
          existing.NeutralPosts = neutral;
          existing.UnratedPosts = unrated;
          existing.ModelName = model.Name;
        }

        await db.SaveChangesAsync(token).ConfigureAwait(false);
        date = date.AddDays(1);
      }
    }
    catch
    {
    }
  }
  private static async Task EvaluateAndSavePostAsync(
    FaGDbContext db,
    Dictionary<Guid, UserPostEvaluation> byId,
    CancellationToken token)
  {
    var evaluator = new SentimentEvaluator();

    foreach (var post in byId.Values)
    {
      try
      {
        post.EvaluationDate = DateTime.UtcNow;
        post.Emotion = SentimentEvaluator.Evaluate(post.PostText ?? string.Empty);

        var exists = await db.UserPostEvaluations
          .FirstOrDefaultAsync(x => x.PostId == post.PostId, token)
          .ConfigureAwait(false);

        if (exists == null)
        {
          await db.UserPostEvaluations.AddAsync(post, token).ConfigureAwait(false);
        }
        else
        {
          exists.Emotion = post.Emotion;
          exists.PostDate = post.PostDate != default ? post.PostDate : exists.PostDate;
          exists.EvaluationDate = post.EvaluationDate;
          exists.PostText = post.PostText ?? exists.PostText;
          exists.CommentsCount = post.CommentsCount;
          exists.TotalReactions = post.TotalReactions;
          exists.ReactionsJson = post.ReactionsJson ?? exists.ReactionsJson;
          exists.AuthorId = post.AuthorId != Guid.Empty ? post.AuthorId : exists.AuthorId;
          exists.AuthorNickname = string.IsNullOrEmpty(post.AuthorNickname) ? exists.AuthorNickname : post.AuthorNickname;
          exists.Tickers = string.IsNullOrEmpty(post.Tickers) ? exists.Tickers : post.Tickers;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      if (token.IsCancellationRequested)
        break;
    }

    await db.SaveChangesAsync(token).ConfigureAwait(false);
  }
  private static Dictionary<Guid, UserPostEvaluation> ClearPosts(List<UserPostEvaluation> allPosts)
  {
    var byId = new Dictionary<Guid, UserPostEvaluation>();
    foreach (var post in allPosts)
    {
      if (post.PostId == Guid.Empty)
        continue;

      if (!byId.TryGetValue(post.PostId, out var _))
        byId.Add(post.PostId, post);
      else
      {
        byId[post.PostId] = post;
      }
    }

    return byId;
  }
  private static async Task<List<UserPostEvaluation>> DownloadAllPostsAsync(
    DateTime startUtc,
    DateTime endUtc,
    List<IFagDownloader> downloaders,
    CancellationToken token)
  {
    var allPosts = new List<UserPostEvaluation>();

    foreach (var dl in downloaders)
    {
      try
      {
        var posts = await dl.DownloadPostsAsync(startUtc, endUtc, token).ConfigureAwait(false);
        if (posts != null && posts.Count > 0)
          allPosts.AddRange(posts);
      }
      catch
      {
      }

      if (token.IsCancellationRequested)
        break;
    }

    return allPosts;
  }
}

/// <summary>
/// Very small sentiment evaluator stub. Replace with real model integration later.
/// </summary>
internal class SentimentEvaluator
{
  private static readonly string[] Positive = ["хорош", "отлич", "класс", "👍", "супер", "любл", "профит", "прибыл"];
  private static readonly string[] Negative = ["плох", "ужас", "⚠", "потер", "убыт", "страш", "жоп", "хз"];

  public static Emotion Evaluate(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
      return Emotion.None;

    var t = text.ToLowerInvariant();
    var pos = Positive.Count(p => t.Contains(p));
    var neg = Negative.Count(n => t.Contains(n));

    if (pos == 0 && neg == 0)
      return Emotion.Neutral;

    if (pos >= neg)
      return Emotion.Positive;

    return Emotion.Negative;
  }
}
