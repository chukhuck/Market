using Cronos;
using FaG.Data;
using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Text.Json;
using TPulse.Client;
using TPulse.Client.Model;

public class Program
{
  public static async Task Main(string[] args)
  {
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
          services.AddHttpClient("FaGClient");
          services.AddSingleton(new TPulseApiClient(
                  "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed",
                  "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/post/instrument/{tiker}",
                  "https://pulse-image-post.cdn-tinkoff.ru/{guid_image}-small.jpeg"
              ));

          // Read DB connection from environment to support docker volumes
          var fagConn = Environment.GetEnvironmentVariable("FAG_DB") ?? "Data Source=fag.db";
          services.AddDbContext<FaGDbContext>(options => options.UseSqlite(fagConn));

          services.AddHostedService<ScheduledWorker>();
        })
        .Build();

    await host.RunAsync();
  }
}

public class ScheduledWorker(IServiceProvider provider, TPulseApiClient pulseClient, IHttpClientFactory factory, IConfiguration configuration) : BackgroundService
{
  private readonly IServiceProvider _provider = provider;
  private readonly TPulseApiClient _pulseClient = pulseClient;
  private readonly HttpClient _httpClient = factory.CreateClient();


  private readonly string _cron = configuration["Scheduler:Cron"] ?? "0 0 * * *";
  private readonly int _windowHours = int.TryParse(configuration["Scheduler:WindowHours"], out var wh) ? wh : 24;
  private readonly DateTime? _initialEnd = DateTime.TryParse(configuration["Scheduler:InitialEndDate"], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : null;
  private readonly string _stateFileName = configuration["Scheduler:StateFile"] ?? "scheduler.state.json";
  private readonly string _stateFilePath = Path.Combine(AppContext.BaseDirectory, configuration["Scheduler:StateFile"] ?? "scheduler.state.json");

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var cronExpr = CronExpression.Parse(_cron, CronFormat.Standard);
    while (!stoppingToken.IsCancellationRequested)
    {
      var now = DateTime.UtcNow;
      var next = cronExpr.GetNextOccurrence(now, TimeZoneInfo.Utc);
      if (next == null)
        break;

      var delay = next.Value - now;
      if (delay > TimeSpan.Zero)
        await Task.Delay(delay, stoppingToken);

      await DoWorkAsync(stoppingToken);
    }
  }

  private DateTime? LoadLastProcessedEndUtc()
  {
    try
    {
      if (!File.Exists(_stateFilePath))
        return null;

      var json = File.ReadAllText(_stateFilePath);
      var state = JsonSerializer.Deserialize<State>(json);
      return state?.LastProcessedEndUtc;
    }
    catch
    {
      return null;
    }
  }

  private void SaveLastProcessedEndUtc(DateTime dt)
  {
    var state = new State { LastProcessedEndUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc) };
    var json = JsonSerializer.Serialize(state);
    File.WriteAllText(_stateFilePath, json);
  }

  private async Task DoWorkAsync(CancellationToken token)
  {
    using var scope = _provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FaGDbContext>();

    var loaded = LoadLastProcessedEndUtc();
    DateTime lastProcessedEndUtc;
    if (loaded.HasValue)
    {
      lastProcessedEndUtc = DateTime.SpecifyKind(loaded.Value, DateTimeKind.Utc);
    }
    else if (_initialEnd.HasValue)
    {
      lastProcessedEndUtc = _initialEnd.Value.AddHours(-_windowHours);
    }
    else
    {
      lastProcessedEndUtc = DateTime.UtcNow.AddHours(-_windowHours);
    }

    // Prepare windows from lastProcessedEndUtc forward up to now
    var nowUtc = DateTime.UtcNow;
    var windows = new List<DateTime>();
    var nextEnd = lastProcessedEndUtc.AddHours(_windowHours);
    while (nextEnd <= nowUtc)
    {
      windows.Add(nextEnd);
      nextEnd = nextEnd.AddHours(_windowHours);
    }

    if (windows.Count == 0)
      return;

    // Buckets for windows (keyed by window end)
    var buckets = windows.ToDictionary(w => w, w => new List<Post>());

    // Stream pages newest->older and distribute posts into buckets.
    string? cursor = null;
    bool stopFetching = false;

    while (!token.IsCancellationRequested && !stopFetching)
    {
      var response = await _pulseClient.GetPostsAsync(100, cursor);
      var posts = response.Payload?.Items;
      if (posts == null || posts.Count == 0)
        break;

      foreach (var post in posts)
      {
        if (post == null)
          continue;

        DateTime insertedUtc = post.Inserted.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(post.Inserted, DateTimeKind.Utc) : post.Inserted.ToUniversalTime();

        if (insertedUtc <= lastProcessedEndUtc)
        {
          // we've reached posts older or equal to the last processed boundary - nothing more to fetch
          stopFetching = true;
          break;
        }

        // Calculate which window this post belongs to. idx = ceil((inserted - lastEnd) / windowHours)
        var hoursDiff = (insertedUtc - lastProcessedEndUtc).TotalHours;
        var idx = (int)Math.Ceiling(hoursDiff / _windowHours);
        if (idx <= 0)
          continue;

        if (idx > windows.Count)
        {
          // post is newer than our newest window end (could happen due to clock skew) - attach to newest window
          idx = windows.Count;
        }

        var targetEnd = lastProcessedEndUtc.AddHours(idx * _windowHours);
        if (buckets.TryGetValue(targetEnd, out var list))
        {
          list.Add(post);
        }
      }

      if (stopFetching)
        break;

      cursor = response.Payload?.NextCursor;
      if (string.IsNullOrEmpty(cursor))
        break;
    }

    // Flush windows in order from oldest to newest
    foreach (var targetEnd in windows)
    {
      var list = buckets[targetEnd];
      if (list.Count > 0)
      {
        // Map and save
        await db.UserPostEvaluations.AddRangeAsync(list.Select(p => p.ToPostEvaluation(Emotion.None)));
        await db.SaveChangesAsync(token);
      }

      // advance state to this window end even if no posts
      SaveLastProcessedEndUtc(targetEnd);
      lastProcessedEndUtc = targetEnd;
    }
  }

  private class State
  {
    public DateTime LastProcessedEndUtc { get; set; }
  }
}
