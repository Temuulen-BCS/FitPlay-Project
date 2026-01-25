using Microsoft.EntityFrameworkCore;
using FitPlay.Domain.Model;

namespace FitPlay.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Box> Boxes { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Training> Trainings { get; set; }
        public DbSet<TrainingExercise> TrainingExercises { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Box configuration
            modelBuilder.Entity<Box>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            // Level configuration
            modelBuilder.Entity<Level>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();

                // User -> Box (Many-to-One)
                entity.HasOne(e => e.Box)
                    .WithMany(b => b.Users)
                    .HasForeignKey(e => e.BoxId)
                    .OnDelete(DeleteBehavior.SetNull);

                // User -> Level (Many-to-One)
                entity.HasOne(e => e.Level)
                    .WithMany(l => l.Users)
                    .HasForeignKey(e => e.LevelId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Exercise configuration
            modelBuilder.Entity<Exercise>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.VideoUrl).HasMaxLength(500);
                entity.Property(e => e.MuscleGroup).HasMaxLength(100);
            });

            // Training configuration
            modelBuilder.Entity<Training>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);

                // Training -> Box (Many-to-One)
                entity.HasOne(e => e.Box)
                    .WithMany(b => b.Trainings)
                    .HasForeignKey(e => e.BoxId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Training -> Trainer (Many-to-One)
                entity.HasOne(e => e.Trainer)
                    .WithMany(u => u.TrainingsAsTrainer)
                    .HasForeignKey(e => e.TrainerId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Training <-> Athletes (Many-to-Many)
                entity.HasMany(e => e.Athletes)
                    .WithMany(u => u.TrainingsAsAthlete)
                    .UsingEntity<Dictionary<string, object>>(
                        "TrainingAthlete",
                        j => j.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                        j => j.HasOne<Training>().WithMany().HasForeignKey("TrainingId").OnDelete(DeleteBehavior.Cascade)
                    );
            });

            // TrainingExercise configuration (Join table with additional data)
            modelBuilder.Entity<TrainingExercise>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Group).HasMaxLength(50);
                entity.Property(e => e.Repetitions).HasMaxLength(50);
                entity.Property(e => e.Weight).HasMaxLength(50);
                entity.Property(e => e.RestTime).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // TrainingExercise -> Training (Many-to-One)
                entity.HasOne(e => e.Training)
                    .WithMany(t => t.TrainingExercises)
                    .HasForeignKey(e => e.TrainingId)
                    .OnDelete(DeleteBehavior.Cascade);

                // TrainingExercise -> Exercise (Many-to-One)
                entity.HasOne(e => e.Exercise)
                    .WithMany(ex => ex.TrainingExercises)
                    .HasForeignKey(e => e.ExerciseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial Levels
            modelBuilder.Entity<Level>().HasData(
                new Level { Id = 1, Name = "Beginner", RequiredXp = 0, Description = "Just starting your fitness journey" },
                new Level { Id = 2, Name = "Intermediate", RequiredXp = 1000, Description = "Building solid foundations" },
                new Level { Id = 3, Name = "Advanced", RequiredXp = 5000, Description = "Pushing your limits" },
                new Level { Id = 4, Name = "Elite", RequiredXp = 15000, Description = "Top performer" },
                new Level { Id = 5, Name = "Legend", RequiredXp = 50000, Description = "Fitness legend status" }
            );
        }
    }
}
