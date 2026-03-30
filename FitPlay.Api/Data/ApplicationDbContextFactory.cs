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
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(connectionString); 

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
