using Microsoft.EntityFrameworkCore;

namespace FaG.Data.DAL
{
  public class FaGDbContext : DbContext
  {
    public DbSet<UserPostEvaluation> UserPostEvaluations { get; set; }
    public DbSet<FearGreedIndex> FearGreedIndices { get; set; }

    public FaGDbContext(DbContextOptions<FaGDbContext> options)
        : base(options)
    {
    }

    public FaGDbContext() : base()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
    }
    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //  optionsBuilder.UseSqlite("Data Source=fag.db");
    //}
  }
}
