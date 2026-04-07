using FaG.Data.Common;
using FaG.Data.DAL;
using FaG.Data.IndexModel;
using FaG.Scheduler.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using TPulse.Client;
using TPulse.Client.Model;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddHttpClient("FaGClient");
builder.Services.AddSingleton(new TPulseApiClient(
    "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed?appName=invest&origin=web&platform=web&include=all",
    "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/post/instrument/{tiker}?appName=invest&origin=web&platform=web&include=all",
    "https://pulse-image-post.cdn-tinkoff.ru/{guid_image}-small.jpeg"
));


builder.Services.AddHttpClient<IMoexDataService, MoexDataService>(client =>
{
  client.DefaultRequestHeaders.UserAgent.ParseAdd("FaG.Moex/1.0");
});

var fagConn = Environment.GetEnvironmentVariable("FAG_DB") ??
    "Host=localhost;Port=5432;Database=fagdb;Username=faguser;Password=fagpassword";

builder.Services.AddDbContext<FaGDbContext>(options => options.UseNpgsql(fagConn));

// Register available downloaders
builder.Services.AddTransient<IFagDownloader, TPulseDownloader>();



var emotionApiUrl = builder.Configuration["EMOTION_API_URL"]
    ?? "http://emotionapi:8080";

builder.Services.AddHttpClient<IFagEvaluater, ApiFagEvaluaterV1>((provider, client) =>
{
  client.Timeout = TimeSpan.FromSeconds(30);
  client.BaseAddress = new Uri(emotionApiUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
  ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

//builder.Services.AddSingleton(new ApiFagEvaluaterV1(
//    new HttpClient(),
//    emotionApiUrl));

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

app.MapGet("/evaluateandindex", async (HttpContext ctx) =>
{
  var q = ctx.Request.Query;
  if (!q.TryGetValue("start", out var startVals) || !q.TryGetValue("end", out var endVals))
    return Results.BadRequest("Требуются параметры 'start' и 'end'");

  if (!TryParseDate(startVals[0], out var start) || !TryParseDate(endVals[0], out var end))
    return Results.BadRequest("Невозможно распарсить даты. Используйте формат 2023-01-01T00:00:00Z.");

  try
  {
    await Program.EvaluatePostsAndBuildIndexByRangeAsync(ctx.RequestServices, start, end, ctx.RequestAborted).ConfigureAwait(false);
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

app.MapGet("/buildindex", async (HttpContext ctx) =>
{
  var q = ctx.Request.Query;
  if (!q.TryGetValue("start", out var startVals) || !q.TryGetValue("end", out var endVals))
    return Results.BadRequest("Требуются параметры 'start' и 'end'");

  if (!TryParseDate(startVals[0], out var start) || !TryParseDate(endVals[0], out var end))
    return Results.BadRequest("Невозможно распарсить даты. Используйте формат 2023-01-01T00:00:00Z.");

  try
  {
    await Program.BuildIndexByRangeAsync(ctx.RequestServices, start, end, ctx.RequestAborted).ConfigureAwait(false);
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

    var moexService = scope.ServiceProvider.GetRequiredService<IMoexDataService>();
    await LoadIMOEXIndexDataAsync(startUtc, endUtc, db, moexService, token);


    
    var end = endUtc.Date;
    var start = end.Date.AddDays(-1);

    while (start >= startUtc.Date)
    {
      var posts = await DownloadAndSaveAllPostsAsync(db, start, end, downloaders, token).ConfigureAwait(false);

      if (posts is null)
        break;

      var evoluations = await EvaluateAndSavePostAsync(db, posts, evaluaters, token).ConfigureAwait(false);
      await CalculateAndSaveFearAndGreadIndexAsync(db, start, evoluations, token).ConfigureAwait(false);
      if (token.IsCancellationRequested)
        break;
      end = end.AddDays(-1);
      start = end.AddDays(-1);
    }
  }

  private static async Task LoadIMOEXIndexDataAsync(DateTime startUtc, DateTime endUtc, FaGDbContext db, IMoexDataService moexService, CancellationToken token)
  {
    Console.WriteLine($"Loading IMOEX data from {startUtc:yyyy-MM-dd} to {endUtc:yyyy-MM-dd}...");
    try
    {
      var imoexData = await moexService.GetIMOEXDataAsync(startUtc, endUtc, token);

      foreach (var day in imoexData)
      {
        var existing = await db.IMOEXIndex
            .FirstOrDefaultAsync(i => i.Date.Date == day.Date.ToUniversalTime().Date, token);

        if (existing == null)
        {
          await db.IMOEXIndex.AddAsync(day, token);
          Console.WriteLine($"Added IMOEX data for {day.Date:yyyy-MM-dd}");
        }
        else
        {
          existing.Open = day.Open;
          existing.Close = day.Close;
          existing.High = day.High;
          existing.Low = day.Low;
          existing.Volume = day.Volume;
          Console.WriteLine($"Updated IMOEX data for {day.Date:yyyy-MM-dd}");
        }
      }

      await db.SaveChangesAsync(token);
      Console.WriteLine($"Loaded {imoexData.Count} IMOEX records");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading IMOEX data: {ex.Message}");
    }
  }

  private static async Task CalculateAndSaveFearAndGreadIndexAsync(
    FaGDbContext db,
    DateTime date,
    List<PostEvaluation> evaluations,
    CancellationToken token)
  {
    Console.WriteLine($"Building FaG index...");
    try
    {
      if (evaluations == null || evaluations.Count == 0)
        return;

      var historicalData = await db.FearGreedIndices
        .Where(i => i.Date > date.AddDays(-SimpleIndexModel.NormalizationWindow - 1))
        .OrderByDescending(i => i.Date)
        .ToListAsync(token)
        .ConfigureAwait(false);

      var model = new SimpleIndexModel(historicalData);



      
      var fearGreedIndex = model.CalculateForDay(evaluations);
      fearGreedIndex.Date = date.ToUniversalTime();
      db.FearGreedIndices.Add( fearGreedIndex );

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
    Console.WriteLine($"Evaluating posts...");

    List<PostEvaluation> evaluations = [];

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
    List<UserPost> allPosts = [];

    foreach (var dl in downloaders)
    {
      try
      {
        List<UserPost> posts = await dl.DownloadPostsAsync(startUtc, endUtc, token).ConfigureAwait(false);

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

  private static async Task EvaluatePostsAndBuildIndexByRangeAsync(IServiceProvider services, DateTime startUtc, DateTime endUtc, CancellationToken token)
  {
    using var scope = services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<FaGDbContext>();

    var evaluaters = scope.ServiceProvider.GetServices<IFagEvaluater>().ToList();


    var start = startUtc.Date;
    var end = start.AddDays(1).Date;

    while (end <= endUtc.Date)
    {
      Console.WriteLine($"Processing posts from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}...");

      var posts = await db.Posts
        .Where(p => p.Date >= start.Date && p.Date < end.Date)
        .Include(p => p.Evaluations)
        .ToListAsync(token)
        .ConfigureAwait(false);


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

  private static async Task BuildIndexByRangeAsync(IServiceProvider services, DateTime startUtc, DateTime endUtc, CancellationToken token)
  {
    using var scope = services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<FaGDbContext>();

    var start = startUtc.Date;
    var end = start.AddDays(1).Date;

    while (end <= endUtc.Date)
    {
      Console.WriteLine($"Processing posts from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}...");
      var posts = await db.Posts
          .Where(p => p.Date >= start.Date.ToUniversalTime() && p.Date < end.Date.ToUniversalTime())
          .Include(p => p.Evaluations)
          .ToListAsync(token)
          .ConfigureAwait(false);

      if (posts is null)
        break;

      var evoluations = posts.SelectMany(p=>p.Evaluations).ToList();
      await CalculateAndSaveFearAndGreadIndexAsync(db, start, evoluations, token).ConfigureAwait(false);
      if (token.IsCancellationRequested)
        break;
      start = end;
      end = start.AddDays(1);
    }
  }
}
