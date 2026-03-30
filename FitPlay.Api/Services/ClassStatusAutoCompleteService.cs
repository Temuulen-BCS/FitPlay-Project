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

            var schedulesToComplete = await db.ClassSchedules
                .Include(s => s.RoomBooking)
                .Where(s => s.Status == ClassScheduleStatus.Scheduled
                    && s.RoomBooking != null
                    && s.RoomBooking.EndTime <= now)
                .ToListAsync(cancellationToken);

            foreach (var schedule in schedulesToComplete)
            {
                schedule.Status = ClassScheduleStatus.Completed;
            }

            var sessionsToComplete = await db.ClassSessions
                .Where(s => s.Status != ClassSessionStatus.Completed
                    && s.Status != ClassSessionStatus.Cancelled
                    && s.EndTime <= now)
                .ToListAsync(cancellationToken);

            foreach (var session in sessionsToComplete)
            {
                session.Status = ClassSessionStatus.Completed;
            }

            var updates = schedulesToComplete.Count + sessionsToComplete.Count;
            if (updates == 0)
            {
                return;
            }

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Auto-completed expired classes: {ScheduleCount} schedules and {SessionCount} sessions at {UtcNow}.",
                schedulesToComplete.Count,
                sessionsToComplete.Count,
                now);
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
