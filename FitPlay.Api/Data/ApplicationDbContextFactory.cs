using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitPlay.Api.Data
{
    /// <summary>
    /// Design-time factory used by "dotnet ef migrations add".
    /// The connection string is a dummy — EF only needs it to build the model, not to connect.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Database=fitplay_design;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly("FitPlay.Api"));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
