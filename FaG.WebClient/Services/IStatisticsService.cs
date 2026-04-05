namespace FaG.WebClient.Services
{
  public interface IStatisticsService
  {
    Task<StatisticsResult> GetStatisticsAsync();

    Task<bool> ClearDatabaseAsync();
  }

  public class StatisticsResult
  {
    public int PostTotalCount { get; set; }
    public int EvaluationTotalCount { get; set; } 
    public DateTime? PostMinDate { get; set; }
    public DateTime? PostMaxDate { get; set; }
    public DateTime? IndexMinDate { get; set; }
    public DateTime? IndexMaxDate { get; set; }
  }
}
