using Microsoft.EntityFrameworkCore;
using FaG.Data.DAL;

namespace TPulse.Trainer.DAL
{
  public class AppDbContext : DbContext
  {
    public DbSet<UserPost> Posts { get; set; } = null!;
    public DbSet<PostEvaluation> Evaluations { get; set; } = null!;


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
