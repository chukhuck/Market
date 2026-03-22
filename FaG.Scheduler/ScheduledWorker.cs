using Cronos;

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

    _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
  }
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var nowLocal = DateTime.UtcNow;
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

      var occLocal = next.Value;
      var startLocal = occLocal.Date.AddDays(-1);
      var endLocal = occLocal.Date;

      var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _timeZone);
      var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, _timeZone);

      await Program.ProcessPostsByRangeAsync(_provider, startUtc, endUtc, stoppingToken).ConfigureAwait(false);
    }
  }
}
