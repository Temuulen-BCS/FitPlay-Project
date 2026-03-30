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

            // Dummy connection string for design-time EF tooling (migrations add, etc.)
            optionsBuilder.UseNpgsql("Host=localhost;Database=FitPlayDB;Username=postgres;Password=postgres");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
