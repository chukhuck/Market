namespace FaG.WebClient.Services
{
  public interface IStatisticsService
  {
    Task<StatisticsResult> GetStatisticsAsync();

    Task<bool> ClearDatabaseAsync();
  }

  public class StatisticsResult
  {
    public int TotalCount { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
  }
}
