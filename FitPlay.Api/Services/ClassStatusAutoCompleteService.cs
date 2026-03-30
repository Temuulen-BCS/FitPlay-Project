using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Services;

public sealed class ClassStatusAutoCompleteService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClassStatusAutoCompleteService> _logger;

    public ClassStatusAutoCompleteService(
        IServiceScopeFactory scopeFactory,
        ILogger<ClassStatusAutoCompleteService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CompleteExpiredClassesAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CompleteExpiredClassesAsync(stoppingToken);
        }
    }

    private async Task CompleteExpiredClassesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FitPlayContext>();
            var clock = scope.ServiceProvider.GetRequiredService<IClockService>();
            var now = clock.UtcNow;

            // ── REVERSE PASS (mock clock only) ──────────────────────────────────────
            // When the dev clock is moved backward, un-complete any schedules/sessions/
            // enrollments whose end time is now in the future again.
            // This pass is intentionally skipped in production (IsMocked == false) so
            // real completions are never reversed automatically.
            int revertedSchedules = 0, revertedSessions = 0, revertedEnrollments = 0;

            if (clock.IsMocked)
            {
                // Re-activate ClassSchedules whose booking end time is now in the future
                var schedulesToReActivate = await db.ClassSchedules
                    .Include(s => s.RoomBooking)
                    .Where(s => s.Status == ClassScheduleStatus.Completed
                        && s.RoomBooking != null
                        && s.RoomBooking.EndTime > now)
                    .ToListAsync(cancellationToken);

                foreach (var s in schedulesToReActivate)
                    s.Status = ClassScheduleStatus.Scheduled;

                revertedSchedules = schedulesToReActivate.Count;

                // Re-activate ClassSessions whose end time is now in the future
                var sessionsToReActivate = await db.ClassSessions
                    .Where(s => s.Status == ClassSessionStatus.Completed
                        && s.EndTime > now)
                    .ToListAsync(cancellationToken);

                foreach (var s in sessionsToReActivate)
                    s.Status = ClassSessionStatus.Scheduled;

                revertedSessions = sessionsToReActivate.Count;

                // Re-activate ClassEnrollments whose parent session end time is now in the future
                var enrollmentsToReActivate = await db.ClassEnrollments
                    .Include(e => e.ClassSession)
                    .Where(e => e.Status == ClassEnrollmentStatus.Completed
                        && e.ClassSession != null
                        && e.ClassSession.EndTime > now)
                    .ToListAsync(cancellationToken);

                foreach (var e in enrollmentsToReActivate)
                    e.Status = ClassEnrollmentStatus.Confirmed;

                revertedEnrollments = enrollmentsToReActivate.Count;
            }

            // ── FORWARD PASS ────────────────────────────────────────────────────────
            // Mark ClassSchedules as Completed when their room booking has ended.
            var schedulesToComplete = await db.ClassSchedules
                .Include(s => s.RoomBooking)
                .Where(s => s.Status == ClassScheduleStatus.Scheduled
                    && s.RoomBooking != null
                    && s.RoomBooking.EndTime <= now)
                .ToListAsync(cancellationToken);

            foreach (var schedule in schedulesToComplete)
                schedule.Status = ClassScheduleStatus.Completed;

            // Mark ClassSessions as Completed when their end time has passed.
            var sessionsToComplete = await db.ClassSessions
                .Where(s => s.Status != ClassSessionStatus.Completed
                    && s.Status != ClassSessionStatus.Cancelled
                    && s.EndTime <= now)
                .ToListAsync(cancellationToken);

            foreach (var session in sessionsToComplete)
                session.Status = ClassSessionStatus.Completed;

            var totalChanges = revertedSchedules + revertedSessions + revertedEnrollments
                             + schedulesToComplete.Count + sessionsToComplete.Count;

            if (totalChanges == 0)
                return;

            await db.SaveChangesAsync(cancellationToken);

            if (revertedSchedules + revertedSessions + revertedEnrollments > 0)
                _logger.LogInformation(
                    "[MockClock] Reverted expired classes: {S} schedules, {Ss} sessions, {E} enrollments at {UtcNow}.",
                    revertedSchedules, revertedSessions, revertedEnrollments, now);

            if (schedulesToComplete.Count + sessionsToComplete.Count > 0)
                _logger.LogInformation(
                    "Auto-completed expired classes: {ScheduleCount} schedules and {SessionCount} sessions at {UtcNow}.",
                    schedulesToComplete.Count, sessionsToComplete.Count, now);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application is shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-complete expired classes.");
        }
    }
}
