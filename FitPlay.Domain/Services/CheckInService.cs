using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class CheckInService : ICheckInService
{
    private const int DefaultCheckInXp = 25;

    private readonly FitPlayContext _db;
    private readonly ProgressService _progressService;
    private readonly AchievementService _achievementService;
    private readonly IClockService _clock;

    public CheckInService(
        FitPlayContext db,
        ProgressService progressService,
        AchievementService achievementService,
        IClockService clock)
    {
        _db = db;
        _progressService = progressService;
        _achievementService = achievementService;
        _clock = clock;
    }

    public async Task<RoomCheckInResponseDto> CheckInAsync(int enrollmentId, string userId, CreateRoomCheckInRequest request)
    {
        var normalizedUserId = userId.Trim();

        var enrollment = await _db.ClassEnrollments
            .Include(e => e.ClassSession)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment is null)
            throw new ArgumentException("Enrollment not found.");

        if (enrollment.UserId != normalizedUserId)
            throw new UnauthorizedAccessException("You can only check in your own enrollment.");

        if (enrollment.Status != ClassEnrollmentStatus.Confirmed && enrollment.Status != ClassEnrollmentStatus.Completed)
            throw new InvalidOperationException("Enrollment payment is not confirmed.");

        var session = enrollment.ClassSession;
        if (session is null)
            throw new InvalidOperationException("Session not found for enrollment.");

        var now = _clock.UtcNow;
        if (now < session.StartTime || now > session.EndTime)
            throw new InvalidOperationException("Check-in is only allowed during class time.");

        var alreadyCheckedIn = await _db.RoomCheckIns
            .AnyAsync(ci => ci.ClassEnrollmentId == enrollmentId && ci.UserId == normalizedUserId);

        if (alreadyCheckedIn)
            throw new InvalidOperationException("User is already checked in for this enrollment.");

        var checkIn = new RoomCheckIn
        {
            ClassEnrollmentId = enrollmentId,
            UserId = normalizedUserId,
            CheckInTime = now,
            XpAwarded = DefaultCheckInXp
        };

        _db.RoomCheckIns.Add(checkIn);
        enrollment.Status = ClassEnrollmentStatus.Completed;

        var domainUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == normalizedUserId);

        if (domainUser is not null)
        {
            var (newTotalXp, newLevel, leveledUp) = await _progressService.AddXpAsync(
                domainUser.Id,
                DefaultCheckInXp,
                XpTransactionType.Adjustment,
                sourceId: enrollmentId,
                reason: $"Class check-in: {session.Title}");

            await _achievementService.CheckAndAwardAchievementsAsync(domainUser.Id, newLevel, leveledUp);
        }

        await _db.SaveChangesAsync();

        return ToDto(checkIn);
    }

    public async Task<List<RoomCheckInResponseDto>> GetSessionCheckInsAsync(int sessionId)
    {
        var checkIns = await _db.RoomCheckIns.AsNoTracking()
            .Where(ci => ci.ClassEnrollment != null && ci.ClassEnrollment.ClassSessionId == sessionId)
            .OrderBy(ci => ci.CheckInTime)
            .ToListAsync();

        return checkIns.Select(ToDto).ToList();
    }

    private static RoomCheckInResponseDto ToDto(RoomCheckIn checkIn) => new(
        checkIn.Id,
        checkIn.ClassEnrollmentId,
        checkIn.UserId,
        checkIn.CheckInTime,
        checkIn.XpAwarded
    );
}
