using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Data;

public class FitPlayContext : DbContext
{
    public FitPlayContext(DbContextOptions<FitPlayContext> options) : base(options) { }

    // Core entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    
    // Exercise & Training
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingExercise> TrainingExercises => Set<TrainingExercise>();
    
    // Gamification
    public DbSet<UserLevel> UserLevels => Set<UserLevel>();
    public DbSet<LevelDefinition> LevelDefinitions => Set<LevelDefinition>();
    public DbSet<TrainingCompletion> TrainingCompletions => Set<TrainingCompletion>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    
    // Legacy (kept for compatibility)
    public DbSet<Ranking> Rankings => Set<Ranking>();
    public DbSet<Level> Levels => Set<Level>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        
        // Exercise indexes
        b.Entity<ExerciseLog>().HasIndex(x => new { x.ClientId, x.PerformedAt });
        b.Entity<Exercise>().HasIndex(x => new { x.TeacherId, x.IsActive });
        
        // Subscription indexes
        b.Entity<Subscription>().HasIndex(x => new { x.ClientId, x.Status });
        
        // Training-Exercise relationship
        b.Entity<TrainingExercise>()
            .HasOne(te => te.Training)
            .WithMany(t => t.Exercises)
            .HasForeignKey(te => te.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);
            
        b.Entity<TrainingExercise>()
            .HasOne(te => te.Exercise)
            .WithMany()
            .HasForeignKey(te => te.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
        
        b.Entity<TrainingExercise>()
            .HasIndex(te => new { te.TrainingId, te.SortOrder });
        
        // Gamification indexes
        b.Entity<UserLevel>()
            .HasIndex(ul => ul.UserId)
            .IsUnique();
            
        b.Entity<TrainingCompletion>()
            .HasIndex(tc => new { tc.UserId, tc.CompletedAt });
            
        b.Entity<TrainingCompletion>()
            .HasIndex(tc => new { tc.TrainingId, tc.Status });
            
        b.Entity<XpTransaction>()
            .HasIndex(xt => new { xt.UserId, xt.CreatedAt });
            
        b.Entity<Achievement>()
            .HasIndex(a => new { a.UserId, a.AchievementType });
    }
}

