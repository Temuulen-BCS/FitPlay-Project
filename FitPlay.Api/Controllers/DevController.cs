using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

/// <summary>
/// Dev-only controller for faking class completions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DevController : ControllerBase
{
    private readonly FitPlayContext _db;
    private readonly ProgressService _progressService;
    private readonly AchievementService _achievementService;
    private readonly IClockService _clock;

    public DevController(FitPlayContext db, ProgressService progressService, AchievementService achievementService, IClockService clock)
    {
        _db = db;
        _progressService = progressService;
        _achievementService = achievementService;
        _clock = clock;
    }

    // ── Time Travel ──

    public record TimeTravelResponse(bool IsMocked, DateTime CurrentTime, string Message);

    /// <summary>
    /// Set the server clock to a specific UTC time. All DateTime.UtcNow calls across the app will use this time.
    /// Example: GET /api/dev/set-time?time=2026-03-28T14:30:00Z
    /// </summary>
    [HttpGet("set-time")]
    public ActionResult<TimeTravelResponse> SetMockTime([FromQuery] DateTime time)
    {
        _clock.SetMockTime(time);
        return Ok(new TimeTravelResponse(true, _clock.UtcNow, $"Time travel enabled. Server time is now {_clock.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"));
    }

    /// <summary>
    /// Reset the server clock to real time.
    /// </summary>
    [HttpGet("reset-time")]
    public ActionResult<TimeTravelResponse> ResetMockTime()
    {
        _clock.Reset();
        return Ok(new TimeTravelResponse(false, _clock.UtcNow, "Time travel disabled. Server is using real time."));
    }

    /// <summary>
    /// Get the current server time (real or mocked).
    /// </summary>
    [HttpGet("time")]
    public ActionResult<TimeTravelResponse> GetCurrentTime()
    {
        var message = _clock.IsMocked
            ? $"MOCKED: Server time is {_clock.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
            : $"REAL: Server time is {_clock.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        return Ok(new TimeTravelResponse(_clock.IsMocked, _clock.UtcNow, message));
    }

    // ── DTOs ──

    public record DevScheduleItem(int Id, int? UserId, string? UserName, string Modality, DateTime ScheduledAt, string Status, string? TrainerName);
    public record DevEnrollmentItem(int Id, int ClassSessionId, string UserId, string? UserName, string SessionTitle, DateTime StartTime, DateTime EndTime, string Status);
    public record DevCompleteRequest(int Id);
    public record DevResetEnrollmentRequest(int Id);
    public record DevCompleteResponse(bool Success, string Message, int XpAwarded);

    // ── List endpoints ──

    /// <summary>
    /// Get ALL ClassSchedule bookings that can be completed (status = Scheduled, has a user).
    /// </summary>
    [HttpGet("schedules")]
    public async Task<ActionResult<List<DevScheduleItem>>> GetAllSchedules()
    {
        var schedules = await _db.ClassSchedules
            .Include(s => s.Trainer)
            .Include(s => s.User)
            .Where(s => s.UserId != null && s.Status == ClassScheduleStatus.Scheduled)
            .OrderByDescending(s => s.ScheduledAt)
            .Select(s => new DevScheduleItem(
                s.Id,
                s.UserId,
                s.User != null ? s.User.Name : null,
                s.Modality,
                s.ScheduledAt,
                s.Status.ToString(),
                s.Trainer != null ? s.Trainer.Name : null
            ))
            .ToListAsync();

        return Ok(schedules);
    }

    /// <summary>
    /// Get ALL ClassEnrollment records that can be completed (status = Confirmed).
    /// </summary>
    [HttpGet("enrollments")]
    public async Task<ActionResult<List<DevEnrollmentItem>>> GetAllEnrollments()
    {
        var enrollments = await _db.ClassEnrollments
            .Include(e => e.ClassSession)
            .Where(e => e.Status == ClassEnrollmentStatus.Confirmed)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        // Resolve user names from domain Users table
        var identityIds = enrollments.Select(e => e.UserId).Distinct().ToList();
        var userMap = await _db.Users
            .Where(u => u.IdentityUserId != null && identityIds.Contains(u.IdentityUserId))
            .ToDictionaryAsync(u => u.IdentityUserId!, u => u.Name);

        var result = enrollments.Select(e => new DevEnrollmentItem(
            e.Id,
            e.ClassSessionId,
            e.UserId,
            userMap.GetValueOrDefault(e.UserId),
            e.ClassSession?.Title ?? "Unknown",
            e.ClassSession?.StartTime ?? DateTime.MinValue,
            e.ClassSession?.EndTime ?? DateTime.MinValue,
            e.Status.ToString()
        )).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get ALL ClassEnrollment records that are already completed (status = Completed).
    /// </summary>
    [HttpGet("completed-enrollments")]
    public async Task<ActionResult<List<DevEnrollmentItem>>> GetCompletedEnrollments()
    {
        var enrollments = await _db.ClassEnrollments
            .Include(e => e.ClassSession)
            .Where(e => e.Status == ClassEnrollmentStatus.Completed)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var identityIds = enrollments.Select(e => e.UserId).Distinct().ToList();
        var userMap = await _db.Users
            .Where(u => u.IdentityUserId != null && identityIds.Contains(u.IdentityUserId))
            .ToDictionaryAsync(u => u.IdentityUserId!, u => u.Name);

        var result = enrollments.Select(e => new DevEnrollmentItem(
            e.Id,
            e.ClassSessionId,
            e.UserId,
            userMap.GetValueOrDefault(e.UserId),
            e.ClassSession?.Title ?? "Unknown",
            e.ClassSession?.StartTime ?? DateTime.MinValue,
            e.ClassSession?.EndTime ?? DateTime.MinValue,
            e.Status.ToString()
        )).ToList();

        return Ok(result);
    }

    // ── Complete endpoints ──

    /// <summary>
    /// Mark a ClassSchedule as completed, preserving its original date and time + award 25 XP.
    /// </summary>
    [HttpPost("complete-schedule")]
    public async Task<ActionResult<DevCompleteResponse>> CompleteSchedule([FromBody] DevCompleteRequest request)
    {
        var schedule = await _db.ClassSchedules.FindAsync(request.Id);
        if (schedule == null)
            return NotFound(new DevCompleteResponse(false, "Schedule not found.", 0));

        if (schedule.Status == ClassScheduleStatus.Completed)
            return BadRequest(new DevCompleteResponse(false, "Schedule is already completed.", 0));

        if (schedule.UserId == null)
            return BadRequest(new DevCompleteResponse(false, "Schedule has no user booked.", 0));

        // Mark as completed — keep ScheduledAt exactly as the trainer set it
        schedule.Status = ClassScheduleStatus.Completed;

        // Award XP
        const int xp = 25;
        var (_, newLevel, leveledUp) = await _progressService.AddXpAsync(
            schedule.UserId.Value,
            xp,
            XpTransactionType.Adjustment,
            sourceId: schedule.Id,
            reason: $"Dev: class completed ({schedule.Modality})"
        );

        await _achievementService.CheckAndAwardAchievementsAsync(schedule.UserId.Value, newLevel, leveledUp);
        await _db.SaveChangesAsync();

        return Ok(new DevCompleteResponse(true, $"Schedule marked as completed on {schedule.ScheduledAt:yyyy-MM-dd HH:mm}. +{xp} XP", xp));
    }

    /// <summary>
    /// Mark a ClassEnrollment as completed, using the session's own start time + award 25 XP.
    /// </summary>
    [HttpPost("complete-enrollment")]
    public async Task<ActionResult<DevCompleteResponse>> CompleteEnrollment([FromBody] DevCompleteRequest request)
    {
        var enrollment = await _db.ClassEnrollments
            .Include(e => e.ClassSession)
            .FirstOrDefaultAsync(e => e.Id == request.Id);

        if (enrollment == null)
            return NotFound(new DevCompleteResponse(false, "Enrollment not found.", 0));

        if (enrollment.Status == ClassEnrollmentStatus.Completed)
        {
            if (enrollment.ClassSession != null && enrollment.ClassSession.Status != ClassSessionStatus.Completed)
            {
                enrollment.ClassSession.Status = ClassSessionStatus.Completed;
                await _db.SaveChangesAsync();
                return Ok(new DevCompleteResponse(true, "Enrollment already completed. Session status synced to Completed.", 0));
            }

            return Ok(new DevCompleteResponse(true, "Enrollment is already completed.", 0));
        }

        // Update enrollment status
        enrollment.Status = ClassEnrollmentStatus.Completed;

        // Also mark the parent session as completed
        if (enrollment.ClassSession != null && enrollment.ClassSession.Status != ClassSessionStatus.Completed)
        {
            enrollment.ClassSession.Status = ClassSessionStatus.Completed;
        }

        // Create RoomCheckIn using the session's own start time
        const int xp = 25;
        var checkInTime = enrollment.ClassSession?.StartTime ?? _clock.UtcNow;
        var checkIn = new RoomCheckIn
        {
            ClassEnrollmentId = enrollment.Id,
            UserId = enrollment.UserId,
            CheckInTime = checkInTime,
            XpAwarded = xp
        };

        // Check for existing check-in to avoid duplicate
        var existingCheckIn = await _db.RoomCheckIns
            .AnyAsync(c => c.ClassEnrollmentId == enrollment.Id && c.UserId == enrollment.UserId);

        if (!existingCheckIn)
        {
            _db.RoomCheckIns.Add(checkIn);
        }

        // Find domain user by identity ID to award XP
        var domainUser = await _db.Users.FirstOrDefaultAsync(u => u.IdentityUserId == enrollment.UserId);
        if (domainUser == null)
        {
            await _db.SaveChangesAsync();
            return Ok(new DevCompleteResponse(true, "Enrollment completed but user not found for XP award.", 0));
        }

        var (_, newLevel, leveledUp) = await _progressService.AddXpAsync(
            domainUser.Id,
            xp,
            XpTransactionType.Adjustment,
            sourceId: enrollment.Id,
            reason: $"Dev: class check-in ({enrollment.ClassSession?.Title ?? "session"})"
        );

        await _achievementService.CheckAndAwardAchievementsAsync(domainUser.Id, newLevel, leveledUp);
        await _db.SaveChangesAsync();

        return Ok(new DevCompleteResponse(true, $"Enrollment completed on {checkInTime:yyyy-MM-dd HH:mm}. +{xp} XP", xp));
    }

    /// <summary>
    /// Reset a ClassEnrollment back to Confirmed and clean up related check-ins.
    /// </summary>
    [HttpPost("reset-enrollment")]
    public async Task<ActionResult<DevCompleteResponse>> ResetEnrollment([FromBody] DevResetEnrollmentRequest request)
    {
        var enrollment = await _db.ClassEnrollments
            .Include(e => e.ClassSession)
            .FirstOrDefaultAsync(e => e.Id == request.Id);

        if (enrollment == null)
            return NotFound(new DevCompleteResponse(false, "Enrollment not found.", 0));

        // Remove any existing check-ins for this enrollment
        var checkIns = await _db.RoomCheckIns
            .Where(c => c.ClassEnrollmentId == enrollment.Id)
            .ToListAsync();

        if (checkIns.Count > 0)
        {
            _db.RoomCheckIns.RemoveRange(checkIns);
        }

        var previousStatus = enrollment.Status;
        enrollment.Status = ClassEnrollmentStatus.Confirmed;

        // If no other completed enrollments remain, roll the session back to Scheduled
        if (enrollment.ClassSession != null)
        {
            var hasOtherCompleted = await _db.ClassEnrollments
                .AnyAsync(e => e.ClassSessionId == enrollment.ClassSessionId && e.Id != enrollment.Id && e.Status == ClassEnrollmentStatus.Completed);

            if (!hasOtherCompleted)
            {
                enrollment.ClassSession.Status = ClassSessionStatus.Scheduled;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new DevCompleteResponse(true, $"Enrollment reset to Confirmed (was {previousStatus}).", 0));
    }

    // ── Database Cleanup ──

    /// <summary>
    /// Delete ALL booking-related data: ClassEnrollments, RoomCheckIns, ClassSessions, ClassSchedules, RoomBookings, and ClassQueues.
    /// WARNING: This is destructive and cannot be undone!
    /// </summary>
    [HttpPost("cleanup-all-bookings")]
    public async Task<ActionResult<object>> CleanupAllBookings()
    {
        try
        {
            // Order matters due to foreign key constraints
            
            // 1. Delete RoomCheckIns (references ClassEnrollments)
            var checkIns = await _db.RoomCheckIns.ToListAsync();
            _db.RoomCheckIns.RemoveRange(checkIns);
            var checkInsCount = checkIns.Count;

            // 2. Delete ClassEnrollments (references ClassSessions)
            var enrollments = await _db.ClassEnrollments.ToListAsync();
            _db.ClassEnrollments.RemoveRange(enrollments);
            var enrollmentsCount = enrollments.Count;

            // 3. Delete ClassQueueEntries (references ClassSchedules)
            var queues = await _db.ClassQueueEntries.ToListAsync();
            _db.ClassQueueEntries.RemoveRange(queues);
            var queuesCount = queues.Count;

            // 4. Delete ClassSchedules (references RoomBookings)
            var schedules = await _db.ClassSchedules.ToListAsync();
            _db.ClassSchedules.RemoveRange(schedules);
            var schedulesCount = schedules.Count;

            // 5. Delete ClassSessions (references RoomBookings)
            var sessions = await _db.ClassSessions.ToListAsync();
            _db.ClassSessions.RemoveRange(sessions);
            var sessionsCount = sessions.Count;

            // 6. Delete RoomBookings (root table)
            var bookings = await _db.RoomBookings.ToListAsync();
            _db.RoomBookings.RemoveRange(bookings);
            var bookingsCount = bookings.Count;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "All booking data has been deleted successfully",
                deleted = new
                {
                    roomCheckIns = checkInsCount,
                    classEnrollments = enrollmentsCount,
                    classQueues = queuesCount,
                    classSchedules = schedulesCount,
                    classSessions = sessionsCount,
                    roomBookings = bookingsCount,
                    total = checkInsCount + enrollmentsCount + queuesCount + schedulesCount + sessionsCount + bookingsCount
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Error during cleanup: {ex.Message}",
                stackTrace = ex.StackTrace
            });
        }
    }
}
