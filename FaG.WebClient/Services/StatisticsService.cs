using FaG.Data; 
using FaG.Data.DAL;
using FaG.WebClient.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FaG.Dashboard.Services
{
  public class StatisticsService : IStatisticsService
  {
    private readonly FaGDbContext _context;

    public StatisticsService(FaGDbContext context)
    {
      _context = context;
    }

    public async Task<bool> ClearDatabaseAsync()
    {

      var entityType = _context.Model.FindEntityType(typeof(UserPostEvaluation));
      var tableName = entityType.GetTableName();  // вернёт "UserPostEvaluations"

      await _context.Database.ExecuteSqlRawAsync(
    $"TRUNCATE TABLE \"{tableName}\"");

      entityType = _context.Model.FindEntityType(typeof(FearGreedIndex));
      tableName = entityType.GetTableName();  // вернёт "UserPostEvaluations"

      await _context.Database.ExecuteSqlRawAsync(
    $"TRUNCATE TABLE \"{tableName}\"");


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
