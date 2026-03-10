using Microsoft.EntityFrameworkCore;
using TPulse.Data;

namespace TPulseHistoryDownloader.DAL
{
  public class HistoryDbContext : DbContext
  {
    public DbSet<UserPostEvaluation> UserPostEvaluations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlite("Data Source=tpulsehistory.db");
    }
  }
}
