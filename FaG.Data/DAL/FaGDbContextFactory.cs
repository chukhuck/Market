using FaG.Data.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FaG.Data
{
  public class FaGDbContextFactory : IDesignTimeDbContextFactory<FaGDbContext>
  {
    public FaGDbContext CreateDbContext(string[] args)
    {
      var optionsBuilder = new DbContextOptionsBuilder<FaGDbContext>();

      // Укажите строку подключения для миграций
      var connectionString = "Host=localhost;Port=5432;Database=fagdb;Username=faguser;Password=fagpassword";

      optionsBuilder.UseNpgsql(connectionString);

      return new FaGDbContext(optionsBuilder.Options);
    }
  }
}
