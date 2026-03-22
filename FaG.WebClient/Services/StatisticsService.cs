using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;

namespace FaG.WebClient.Services
{
  public class StatisticsService(FaGDbContext context) : IStatisticsService
  {
    private readonly FaGDbContext _context = context;

    public async Task<bool> ClearDatabaseAsync()
    {
      var entityType = _context.Model.FindEntityType(typeof(UserPostEvaluation));
      if (entityType != null)
      {
        var tableName = entityType.GetTableName();
        await _context.Database.ExecuteSqlAsync($"TRUNCATE TABLE \"{tableName}\"");
      }

      entityType = _context.Model.FindEntityType(typeof(FearGreedIndex));
      if (entityType != null)
      {
        var tableName = entityType.GetTableName();

        await _context.Database.ExecuteSqlAsync($"TRUNCATE TABLE \"{tableName}\"");
      }


      return true;
    }

    public async Task<StatisticsResult> GetStatisticsAsync()
    {
      var totalCount = await _context.UserPostEvaluations.CountAsync();
      var minDate = await _context.UserPostEvaluations
          .MinAsync(u => u.PostDate);
      var maxDate = await _context.UserPostEvaluations
          .MaxAsync(u => u.PostDate);

      return new StatisticsResult
      {
        TotalCount = totalCount,
        MinDate = minDate,
        MaxDate = maxDate
      };
    }
  }
}
