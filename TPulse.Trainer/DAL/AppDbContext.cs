using Microsoft.EntityFrameworkCore;
using FaG.Data;

namespace TPulse.Trainer.DAL
{
  public class AppDbContext : DbContext
  {
    public DbSet<UserPostEvaluation> UserPostEvaluations { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        var conn = Environment.GetEnvironmentVariable("TRAINER_DB") ?? "Data Source=tpulsetrainer.db";
        optionsBuilder.UseSqlite(conn);
      }
    }
  }
}
