using Cronos;
using FaG.Common;
using FaG.Data.DAL;
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

var fagConn = Environment.GetEnvironmentVariable("FAG_DB") ?? "Data Source=fag.db";
builder.Services.AddDbContext<FaGDbContext>(options => options.UseSqlite(fagConn));

// Register available downloaders
builder.Services.AddTransient<IFagDownloader, TPulseDownloader>();

builder.Services.AddHostedService<ScheduledWorker>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("FaG.Scheduler Web API is running"));

app.MapGet("/process-range", async (HttpContext ctx) =>
{
  var q = ctx.Request.Query;
  if (!q.TryGetValue("start", out var startVals) || !q.TryGetValue("end", out var endVals))
    return Results.BadRequest("Query parameters 'start' and 'end' are required");

  if (!TryParseDate(startVals[0], out var start) || !TryParseDate(endVals[0], out var end))
    return Results.BadRequest("Failed to parse dates. Use ISO format, e.g. 2023-01-01T00:00:00Z.");

  try
  {
    await Program.ProcessRangeAsync(ctx.RequestServices, start, end, ctx.RequestAborted);
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
  public static async Task ProcessRangeAsync(IServiceProvider services, DateTime startUtc, DateTime endUtc, CancellationToken token)
  {
    using var scope = services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<FaGDbContext>();
    var downloaders = scope.ServiceProvider.GetServices<IFagDownloader>().ToList();

    if (downloaders.Count == 0)
      return;

    var allPosts = new List<UserPostEvaluation>();

    foreach (var dl in downloaders)
    {
      try
      {
        var list = await dl.DownloadPostsAsync(startUtc, endUtc, token);
        if (list != null && list.Count > 0)
          allPosts.AddRange(list);
      }
      catch
      {
      }

      if (token.IsCancellationRequested)
        break;
    }

    var byId = new Dictionary<Guid, UserPostEvaluation>();
    foreach (var p in allPosts)
    {
      if (p.PostId == Guid.Empty)
        continue;

      if (!byId.TryGetValue(p.PostId, out var exist))
        byId[p.PostId] = p;
      else
      {
        if (p.EvaluationDate > exist.EvaluationDate)
          byId[p.PostId] = p;
      }
    }

    var evaluator = new SentimentEvaluator();

    foreach (var p in byId.Values)
    {
      try
      {
        p.EvaluationDate = DateTime.UtcNow;
        p.Emotion = evaluator.Evaluate(p.PostText ?? string.Empty);

        var exists = await db.UserPostEvaluations.FirstOrDefaultAsync(x => x.PostId == p.PostId, token);
        if (exists == null)
        {
          await db.UserPostEvaluations.AddAsync(p, token);
        }
        else
        {
          exists.Emotion = p.Emotion;
          exists.EvaluationDate = p.EvaluationDate;
          exists.PostText = p.PostText ?? exists.PostText;
          exists.CommentsCount = p.CommentsCount;
          exists.TotalReactions = p.TotalReactions;
          exists.ReactionsJson = p.ReactionsJson ?? exists.ReactionsJson;
          exists.AuthorId = p.AuthorId != Guid.Empty ? p.AuthorId : exists.AuthorId;
          exists.AuthorNickname = string.IsNullOrEmpty(p.AuthorNickname) ? exists.AuthorNickname : p.AuthorNickname;
          exists.Tickers = string.IsNullOrEmpty(p.Tickers) ? exists.Tickers : p.Tickers;
        }
      }
      catch
      {
      }

      if (token.IsCancellationRequested)
        break;
    }

    await db.SaveChangesAsync(token);
  }
}

public class ScheduledWorker : BackgroundService
{
  private readonly IServiceProvider _provider;
  private readonly CronExpression _cronExpr;
  private readonly TimeZoneInfo _timeZone;

  public ScheduledWorker(IServiceProvider provider, IConfiguration configuration)
  {
    _provider = provider;

    var cron = configuration["Scheduler:Cron"] ?? "15 0 * * *"; // default daily at 00:15
    _cronExpr = CronExpression.Parse(cron, CronFormat.Standard);

    // Assume Moscow timezone
    _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var nowLocal = DateTime.Now;
      var next = _cronExpr.GetNextOccurrence(nowLocal, _timeZone);
      if (next == null)
        break;

      var delay = next.Value - nowLocal;
      if (delay > TimeSpan.Zero)
      {
        try
        {
          await Task.Delay(delay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
          break;
        }
      }

      // compute previous day window based on next occurrence local date
      var occLocal = next.Value;
      var startLocal = occLocal.Date.AddDays(-1);
      var endLocal = occLocal.Date;

      var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
      var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _timeZone);

      await Program.ProcessRangeAsync(_provider, startUtc, endUtc, stoppingToken);
    }
  }
}

/// <summary>
/// Very small sentiment evaluator stub. Replace with real model integration later.
/// </summary>
internal class SentimentEvaluator
{
  private static readonly string[] Positive = new[] { "хорош", "отлич", "класс", "👍", "супер", "любл", "профит", "прибыл" };
  private static readonly string[] Negative = new[] { "плох", "ужас", "⚠", "потер", "убыт", "страш", "жоп", "хз" };

  public Emotion Evaluate(string text)
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
