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

    // Gym / Room Management
    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<GymLocation> GymLocations => Set<GymLocation>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TrainerGymLink> TrainerGymLinks => Set<TrainerGymLink>();
    public DbSet<RoomBooking> RoomBookings => Set<RoomBooking>();
    public DbSet<RoomOperatingHours> RoomOperatingHours => Set<RoomOperatingHours>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<PaymentSplit> PaymentSplits => Set<PaymentSplit>();
    public DbSet<RoomCheckIn> RoomCheckIns => Set<RoomCheckIn>();

    // Exercise & Training
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingExercise> TrainingExercises => Set<TrainingExercise>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();

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

        // Gym indexes and constraints
        b.Entity<Gym>()
            .HasIndex(a => a.CNPJ)
            .IsUnique();

        b.Entity<Gym>()
            .Property(a => a.Name)
            .HasMaxLength(120)
            .IsRequired();

        b.Entity<Gym>()
            .Property(a => a.CNPJ)
            .HasMaxLength(18)
            .IsRequired();

        b.Entity<Gym>()
            .Property(a => a.StripeAccountId)
            .HasMaxLength(255);

        b.Entity<Gym>()
            .Property(a => a.CommissionRate)
            .HasPrecision(5, 4);

        b.Entity<Gym>()
            .Property(a => a.CancelFeeRate)
            .HasPrecision(5, 4);

        b.Entity<GymLocation>()
            .HasIndex(gl => gl.GymId);

        b.Entity<GymLocation>()
            .Property(gl => gl.Name)
            .HasMaxLength(120)
            .IsRequired();

        b.Entity<GymLocation>()
            .Property(gl => gl.Address)
            .HasMaxLength(200)
            .IsRequired();

        b.Entity<GymLocation>()
            .Property(gl => gl.City)
            .HasMaxLength(100)
            .IsRequired();

        b.Entity<GymLocation>()
            .Property(gl => gl.State)
            .HasMaxLength(100)
            .IsRequired();

        b.Entity<GymLocation>()
            .Property(gl => gl.ZipCode)
            .HasMaxLength(20)
            .IsRequired();

        b.Entity<GymLocation>()
            .HasOne(gl => gl.Gym)
            .WithMany(a => a.GymLocations)
            .HasForeignKey(gl => gl.GymId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Room>()
            .HasIndex(r => r.GymLocationId);

        b.Entity<Room>()
            .Property(r => r.Name)
            .HasMaxLength(120)
            .IsRequired();

        b.Entity<Room>()
            .Property(r => r.Description)
            .HasMaxLength(500);

        b.Entity<Room>()
            .Property(r => r.PricePerHour)
            .HasPrecision(18, 2);

        b.Entity<Room>()
            .HasOne(r => r.GymLocation)
            .WithMany(gl => gl.Rooms)
            .HasForeignKey(r => r.GymLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<RoomOperatingHours>()
            .HasIndex(oh => new { oh.RoomId, oh.DayOfWeek })
            .IsUnique();

        b.Entity<RoomOperatingHours>()
            .HasOne(oh => oh.Room)
            .WithMany(r => r.OperatingHours)
            .HasForeignKey(oh => oh.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TrainerGymLink>()
            .HasIndex(tal => new { tal.TrainerId, tal.GymId })
            .IsUnique();

        b.Entity<TrainerGymLink>()
            .Property(tal => tal.TrainerId)
            .HasMaxLength(450)
            .IsRequired();

        b.Entity<TrainerGymLink>()
            .HasOne(tal => tal.Gym)
            .WithMany()
            .HasForeignKey(tal => tal.GymId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<RoomBooking>()
            .HasIndex(rb => new { rb.RoomId, rb.StartTime });

        b.Entity<RoomBooking>()
            .HasIndex(rb => new { rb.RoomId, rb.EndTime });

        b.Entity<RoomBooking>()
            .HasIndex(rb => new { rb.TrainerId, rb.StartTime });

        b.Entity<RoomBooking>()
            .Property(rb => rb.TrainerId)
            .HasMaxLength(450)
            .IsRequired();

        b.Entity<RoomBooking>()
            .Property(rb => rb.PurposeDescription)
            .HasMaxLength(1000);

        b.Entity<RoomBooking>()
            .Property(rb => rb.TotalCost)
            .HasPrecision(18, 2);

        b.Entity<RoomBooking>()
            .Property(rb => rb.Notes)
            .HasMaxLength(2000);

        b.Entity<RoomBooking>()
            .HasOne(rb => rb.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(rb => rb.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ClassSession>()
            .HasIndex(cs => new { cs.TrainerId, cs.StartTime });

        b.Entity<ClassSession>()
            .HasIndex(cs => cs.RoomBookingId)
            .IsUnique();

        b.Entity<ClassSession>()
            .Property(cs => cs.TrainerId)
            .HasMaxLength(450)
            .IsRequired();

        b.Entity<ClassSession>()
            .Property(cs => cs.Title)
            .HasMaxLength(150)
            .IsRequired();

        b.Entity<ClassSession>()
            .Property(cs => cs.Description)
            .HasMaxLength(1000);

        b.Entity<ClassSession>()
            .Property(cs => cs.PricePerStudent)
            .HasPrecision(18, 2);

        b.Entity<ClassSession>()
            .HasOne(cs => cs.RoomBooking)
            .WithOne(rb => rb.ClassSession)
            .HasForeignKey<ClassSession>(cs => cs.RoomBookingId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ClassEnrollment>()
            .HasIndex(ce => new { ce.ClassSessionId, ce.UserId })
            .IsUnique();

        b.Entity<ClassEnrollment>()
            .Property(ce => ce.UserId)
            .HasMaxLength(450)
            .IsRequired();

        b.Entity<ClassEnrollment>()
            .Property(ce => ce.StripePaymentIntentId)
            .HasMaxLength(255);

        b.Entity<ClassEnrollment>()
            .Property(ce => ce.PaidAmount)
            .HasPrecision(18, 2);

        b.Entity<ClassEnrollment>()
            .HasOne(ce => ce.ClassSession)
            .WithMany(cs => cs.Enrollments)
            .HasForeignKey(ce => ce.ClassSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<PaymentSplit>()
            .HasIndex(ps => ps.ClassEnrollmentId)
            .IsUnique();

        b.Entity<PaymentSplit>()
            .Property(ps => ps.GymAmount)
            .HasPrecision(18, 2);

        b.Entity<PaymentSplit>()
            .Property(ps => ps.TrainerAmount)
            .HasPrecision(18, 2);

        b.Entity<PaymentSplit>()
            .Property(ps => ps.PlatformAmount)
            .HasPrecision(18, 2);

        b.Entity<PaymentSplit>()
            .Property(ps => ps.StripeTransferId)
            .HasMaxLength(255);

        b.Entity<PaymentSplit>()
            .HasOne(ps => ps.ClassEnrollment)
            .WithOne(ce => ce.PaymentSplit)
            .HasForeignKey<PaymentSplit>(ps => ps.ClassEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<RoomCheckIn>()
            .HasIndex(ci => new { ci.ClassEnrollmentId, ci.UserId })
            .IsUnique();

        b.Entity<RoomCheckIn>()
            .Property(ci => ci.UserId)
            .HasMaxLength(450)
            .IsRequired();

        b.Entity<RoomCheckIn>()
            .HasOne(ci => ci.ClassEnrollment)
            .WithMany(ce => ce.CheckIns)
            .HasForeignKey(ci => ci.ClassEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exercise indexes
        b.Entity<ExerciseLog>().HasIndex(x => new { x.ClientId, x.PerformedAt });
        b.Entity<ExerciseLog>()
            .HasOne(el => el.Exercise)
            .WithMany()
            .HasForeignKey(el => el.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
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

        b.Entity<ClassSchedule>()
            .HasIndex(ts => new { ts.UserId, ts.ScheduledAt });

        b.Entity<ClassSchedule>()
            .HasIndex(ts => new { ts.TrainerId, ts.ScheduledAt });

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
