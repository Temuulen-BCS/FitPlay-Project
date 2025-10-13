using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Data;

public class FitPlayContext : DbContext
{
    public FitPlayContext(DbContextOptions<FitPlayContext> options) : base(options) { }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ExerciseLog>().HasIndex(x => new { x.ClientId, x.PerformedAt });
        b.Entity<Exercise>().HasIndex(x => new { x.TeacherId, x.IsActive });
        b.Entity<Subscription>().HasIndex(x => new { x.ClientId, x.Status });
    }
}

