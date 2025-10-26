using FitPlay.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
    
    }
}
