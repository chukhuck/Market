using Microsoft.EntityFrameworkCore;

namespace TPulseTrainer.DAL
{
  public class AppDbContext : DbContext
  {
    public DbSet<UserPostEvaluation> UserPostEvaluations { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        // Локальный файл SQLite (создаётся в рабочей директории приложения)
        optionsBuilder.UseSqlite("Data Source=tpulsetrainer.db");
      }
    }
  }
}
