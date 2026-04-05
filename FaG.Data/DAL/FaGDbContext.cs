using Microsoft.EntityFrameworkCore;

namespace FaG.Data.DAL
{
  public class FaGDbContext : DbContext
  {
    public DbSet<UserPost> Posts { get; set; }
    public DbSet<PostEvaluation> Evaluations { get; set; }
    public DbSet<FearGreedIndex> FearGreedIndices { get; set; }
    public DbSet<IMOEXIndexTradeDay> IMOEXIndex { get; set; }

    public FaGDbContext(DbContextOptions<FaGDbContext> options)
        : base(options)
    {
    }

    public FaGDbContext() : base()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<PostEvaluation>()
        .HasOne(pe => pe.Post)                   
        .WithMany(up => up.Evaluations)         
        .HasForeignKey(pe => pe.PostId)      
        .OnDelete(DeleteBehavior.Cascade);      

      base.OnModelCreating(modelBuilder);
    }
  }
}
