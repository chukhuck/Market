using FaG.Data.Common;
using FaG.Data.DAL;
using FaG.Data.IndexModel;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
    var evaluaters = scope.ServiceProvider.GetServices<IFagEvaluater>().ToList();


    var start = startUtc.Date;
    var end = start.AddDays(1).Date;

    while (end <= endUtc.Date.AddDays(1))
    {
      Console.WriteLine($"Processing posts from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}...");
      var posts = await DownloadAndSaveAllPostsAsync(db, start, end, downloaders, token).ConfigureAwait(false);

      if (posts is null)
        break;

      var evoluations = await EvaluateAndSavePostAsync(db, posts, evaluaters, token).ConfigureAwait(false);
      await CalculateAndSaveFearAndGreadIndexAsync(db, start, evoluations, token).ConfigureAwait(false);
      if (token.IsCancellationRequested)
        break;
      start = end;
      end = start.AddDays(1);
    }
  }
  private static async Task CalculateAndSaveFearAndGreadIndexAsync(
    FaGDbContext db,
    DateTime date,
    List<PostEvaluation> evaluations,
    CancellationToken token)
  {
    try
    {

      var previousIndex = await db.FearGreedIndices
        .Where(i => i.Date < date && i.Date >= date.AddDays(-1))
        .OrderByDescending(i => i.Date)
        .FirstOrDefaultAsync(token)
        .ConfigureAwait(false);



      var model = new SimpleIndexModel();
      var fearGreedIndex = model.CalculateForDay(evaluations);
      fearGreedIndex.Date = date;

      await db.SaveChangesAsync(token).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }
  private static async Task<List<PostEvaluation>> EvaluateAndSavePostAsync(
    FaGDbContext db,
    List<UserPost> posts,
    List<IFagEvaluater> evaluaters,
    CancellationToken token)
  {
    List<PostEvaluation> evaluations = new();

    Stopwatch stopwatch = new();

    foreach (var evaluater in evaluaters)
    {
      foreach (var post in posts)
      {
        try
        {
          stopwatch.Restart();
          var evoluation = await evaluater.EvaluateAsync(post, token).ConfigureAwait(false);
          stopwatch.Stop();

          if (evoluation != null)
          {
            evoluation.Longiness = stopwatch.ElapsedMilliseconds;

            post.Evaluations.Add(evoluation);
            db.Evaluations.Add(evoluation);
            evaluations.Add(evoluation);
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.ToString());
        }

        if (token.IsCancellationRequested)
          break;

        await db.SaveChangesAsync(token).ConfigureAwait(false);
      }
    }

    return evaluations;
  }

  private static async Task<List<UserPost>> DownloadAndSaveAllPostsAsync(
    FaGDbContext db,
    DateTime startUtc,
    DateTime endUtc,
    List<IFagDownloader> downloaders,
    CancellationToken token)
  {
    List<UserPost> allPosts = new();

    foreach (var dl in downloaders)
    {
      try
      {
        var posts = await dl.DownloadPostsAsync(startUtc, endUtc, token).ConfigureAwait(false);

        if (posts != null)
        {
          allPosts.AddRange(posts);
          await db.Posts.AddRangeAsync(posts, token).ConfigureAwait(false);
          await db.SaveChangesAsync(token).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
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
