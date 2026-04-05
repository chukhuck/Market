using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;

namespace FaG.WebClient.Services
{
  public class StatisticsService(FaGDbContext context) : IStatisticsService
  {
    private readonly FaGDbContext _context = context;

    public async Task<bool> ClearDatabaseAsync()
    {
      var entityType = _context.Model.FindEntityType(typeof(UserPost));
      if (entityType != null)
      {
        var tableName = entityType.GetTableName();
        await _context.Database.ExecuteSqlAsync($"TRUNCATE TABLE \"{tableName}\"");
      }

      entityType = _context.Model.FindEntityType(typeof(PostEvaluation));
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

    public async Task<bool> ClearEvaluationsAndIndexAsync()
    {
      var entityType = _context.Model.FindEntityType(typeof(PostEvaluation));
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

    public async Task<bool> ClearIndexAsync()
    {
      var entityType = _context.Model.FindEntityType(typeof(FearGreedIndex));
      if (entityType != null)
      {
        var tableName = entityType.GetTableName();

        await _context.Database.ExecuteSqlAsync($"TRUNCATE TABLE \"{tableName}\"");
      }


      return true;
    }

    public async Task<StatisticsResult> GetStatisticsAsync()
    {
      var totalCount = await _context.Posts.CountAsync();
      var evaluationTotalCount = await _context.Evaluations.CountAsync();
      var minDate = await _context.Posts.MinAsync(u => u.Date);
      var maxDate = await _context.Posts.MaxAsync(u => u.Date);

      var indexMinDate = await _context.FearGreedIndices
          .MinAsync(u => u.Date);
      var indexMaxDate = await _context.FearGreedIndices
          .MaxAsync(u => u.Date);

      return new StatisticsResult
      {
        PostTotalCount = totalCount,
        EvaluationTotalCount = evaluationTotalCount,
        PostMinDate = minDate,
        PostMaxDate = maxDate,
        IndexMinDate = indexMinDate,
        IndexMaxDate = indexMaxDate
      };
    }
  }
}
