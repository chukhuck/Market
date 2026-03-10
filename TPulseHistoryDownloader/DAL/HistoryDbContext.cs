using Microsoft.EntityFrameworkCore;
using TPulse.Data;

namespace TPulseHistoryDownloader.DAL
{
  public class HistoryDbContext : DbContext
  {
    public DbSet<UserPostEvaluation> UserPostEvaluations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      // Read connection string from environment to support container volume persistence
      var conn = Environment.GetEnvironmentVariable("HISTORY_DB") ?? "Data Source=tpulsehistory.db";
      optionsBuilder.UseSqlite(conn);
    }
  }
}
