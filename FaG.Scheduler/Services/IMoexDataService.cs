using FaG.Data.DAL;

namespace FaG.Scheduler.Services
{
  public interface IMoexDataService
  {
    Task<List<IMOEXIndexTradeDay>> GetIMOEXDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken token);
  }

}
