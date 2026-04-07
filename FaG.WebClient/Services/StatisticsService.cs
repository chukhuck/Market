using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;

namespace FaG.WebClient.Services
{
  public class StatisticsService(FaGDbContext context) : IStatisticsService
  {
    private readonly FaGDbContext _context = context;

    public async Task<bool> ClearDatabaseAsync()
    {
      try
      {
        var entityType = _context.Model.FindEntityType(typeof(UserPost));
        if (entityType != null)
        {
          var tableName = entityType.GetTableName();
          await _context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\"  CASCADE");
        }

        entityType = _context.Model.FindEntityType(typeof(PostEvaluation));
        if (entityType != null)
        {
          var tableName = entityType.GetTableName();
          await _context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\"");
        }

        entityType = _context.Model.FindEntityType(typeof(FearGreedIndex));
        if (entityType != null)
        {
          var tableName = entityType.GetTableName();

          await _context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\"");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        return false;
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
      var minDate = totalCount == 0 ? DateTime.MinValue : await _context.Posts.MinAsync(u => u.Date);
      var maxDate = totalCount == 0 ? DateTime.MinValue : await _context.Posts.MaxAsync(u => u.Date);

      var fagIndexCount = await _context.FearGreedIndices.CountAsync();
      var indexMinDate = fagIndexCount == 0 ? DateTime.MinValue : await _context.FearGreedIndices
          .MinAsync(u => u.Date);
      var indexMaxDate = fagIndexCount == 0 ? DateTime.MinValue : await _context.FearGreedIndices
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
