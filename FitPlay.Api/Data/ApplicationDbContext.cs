using FitPlay.Domain.model;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Teachers> Teachers  { get; set; }
    
    }
}
