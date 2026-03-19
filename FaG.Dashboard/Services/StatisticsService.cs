using FaG.Dashboard.Services;
using FaG.Data; 
using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;

namespace FaG.Dashboard.Services
{
  public class StatisticsService : IStatisticsService
  {
    private readonly FaGDbContext _context;

    public StatisticsService(FaGDbContext context)
    {
      _context = context;
    }

    public async Task<StatisticsResult> GetStatisticsAsync()
    {
      var totalCount = await _context.UserPostEvaluations.CountAsync();
      var minDate = await _context.UserPostEvaluations
          .MinAsync(u => u.EvaluationDate);
      var maxDate = await _context.UserPostEvaluations
          .MaxAsync(u => u.EvaluationDate);

      return new StatisticsResult
      {
        TotalCount = totalCount,
        MinDate = minDate,
        MaxDate = maxDate
      };
    }
  }
}
