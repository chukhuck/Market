using System.Net.Http.Json;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TPulse.Data;
using TPulse.Client;
using TPulseHistoryDownloader.DAL;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient("PulseClient");
                services.AddSingleton(new TPulseApiClient(
                    "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/feed",
                    "https://www.tbank.ru/mybank/api/social-api-gateway/social/post/feed/v1/post/instrument/{tiker}",
                    "https://pulse-image-post.cdn-tinkoff.ru/{guid_image}-small.jpeg"
                ));

                // Read DB connection from environment to support docker volumes
                var historyConn = Environment.GetEnvironmentVariable("HISTORY_DB") ?? "Data Source=tpulsehistory.db";
                services.AddDbContext<HistoryDbContext>(options => options.UseSqlite(historyConn));

                services.AddHostedService<ScheduledWorker>();
            })
            .Build();

        await host.RunAsync();
    }
}

public class ScheduledWorker(IServiceProvider provider, TPulseApiClient pulseClient, IHttpClientFactory factory) : BackgroundService
{
    private readonly IServiceProvider _provider = provider;
    private readonly TPulseApiClient _pulseClient = pulseClient;
    private readonly HttpClient _httpClient = factory.CreateClient();

    // Cron expression по умолчанию: каждый день в полночь
    private readonly string _cron = "0 0 * * *";

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

    private async Task DoWorkAsync(CancellationToken token)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HistoryDbContext>();

        // Получаем новые посты (простая логика: берём последние 100)
        var posts = (await _pulseClient.GetPostsAsync(100, null)).Payload?.Items ?? [];

        foreach (var post in posts)
        {
            var eval = post.ToPostEvaluation(Emotion.None);
            db.UserPostEvaluations.Add(eval);
        }

        await db.SaveChangesAsync(token);

        // Здесь должна быть логика вычисления индекса страха и жадности и запись в другую БД
    }
}
