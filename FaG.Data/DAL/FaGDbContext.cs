using Microsoft.EntityFrameworkCore;

namespace FaG.Data.DAL
{
  public class FaGDbContext : DbContext
  {
    public DbSet<UserPostEvaluation> UserPostEvaluations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlite("Data Source=fag.db");
    }
  }
}
