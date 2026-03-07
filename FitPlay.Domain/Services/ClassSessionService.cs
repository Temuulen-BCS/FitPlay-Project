using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class ClassSessionService : IClassSessionService
{
    private readonly FitPlayContext _db;

    public ClassSessionService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<ClassSessionResponseDto?> GetSessionByIdAsync(int sessionId)
    {
        var session = await _db.ClassSessions.AsNoTracking()
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return session is null ? null : ToDto(session);
    }

    public async Task<List<ClassSessionResponseDto>> GetTrainerSessionsAsync(string trainerId, DateTime? from = null, DateTime? to = null)
    {
        var normalizedTrainerId = trainerId.Trim();
        var query = _db.ClassSessions.AsNoTracking()
            .Include(s => s.Enrollments)
            .Where(s => s.TrainerId == normalizedTrainerId)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(s => s.EndTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.StartTime <= to.Value);
        }

        var sessions = await query.OrderByDescending(s => s.StartTime).ToListAsync();
        return sessions.Select(ToDto).ToList();
    }

    public async Task<ClassSessionResponseDto> CreateSessionAsync(int bookingId, string trainerId, CreateClassSessionRequest request)
    {
        var normalizedTrainerId = trainerId.Trim();

        var booking = await _db.RoomBookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            throw new ArgumentException("Room booking not found.");

        if (booking.TrainerId != normalizedTrainerId)
            throw new UnauthorizedAccessException("You can only create sessions from your own bookings.");

        if (booking.Status != RoomBookingStatus.Confirmed)
            throw new InvalidOperationException("ClassSession can only be created from confirmed RoomBooking.");

        ValidateTimeRange(request.StartTime, request.EndTime);

        if (request.StartTime < booking.StartTime || request.EndTime > booking.EndTime)
            throw new InvalidOperationException("Session time must be inside the room booking interval.");

        var existing = await _db.ClassSessions.AnyAsync(s => s.RoomBookingId == bookingId);
        if (existing)
            throw new InvalidOperationException("A session already exists for this booking.");

        var session = new ClassSession
        {
            RoomBookingId = bookingId,
            TrainerId = normalizedTrainerId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            MaxStudents = request.MaxStudents,
            PricePerStudent = request.PricePerStudent,
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc),
            Status = ClassSessionStatus.Scheduled
        };

        _db.ClassSessions.Add(session);
        await _db.SaveChangesAsync();

        return ToDto(session);
    }

    public async Task<ClassSessionResponseDto?> UpdateSessionAsync(int sessionId, string actorUserId, bool isAdmin, UpdateClassSessionRequest request)
    {
        var session = await _db.ClassSessions
            .Include(s => s.RoomBooking)
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null) return null;

        var normalizedActor = actorUserId.Trim();
        if (!isAdmin && session.TrainerId != normalizedActor)
            throw new UnauthorizedAccessException("You can only update your own sessions.");

        ValidateTimeRange(request.StartTime, request.EndTime);

        if (!Enum.TryParse<ClassSessionStatus>(request.Status, true, out var status))
            throw new ArgumentException("Invalid session status.");

        var booking = session.RoomBooking;
        if (booking is not null)
        {
            if (request.StartTime < booking.StartTime || request.EndTime > booking.EndTime)
                throw new InvalidOperationException("Session time must stay inside the room booking interval.");
        }

        session.Title = request.Title.Trim();
        session.Description = request.Description?.Trim();
        session.MaxStudents = request.MaxStudents;
        session.PricePerStudent = request.PricePerStudent;
        session.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        session.EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);
        session.Status = status;

        await _db.SaveChangesAsync();
        return ToDto(session);
    }

    public async Task<bool> CancelSessionAsync(int sessionId, string actorUserId, bool isAdmin)
    {
        var session = await _db.ClassSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null) return false;

        var normalizedActor = actorUserId.Trim();
        if (!isAdmin && session.TrainerId != normalizedActor)
            throw new UnauthorizedAccessException("You can only cancel your own sessions.");

        session.Status = ClassSessionStatus.Cancelled;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<ClassEnrollmentResponseDto> EnrollAsync(int sessionId, string userId, CreateClassEnrollmentRequest request)
    {
        var normalizedUserId = userId.Trim();

        var session = await _db.ClassSessions
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
            throw new ArgumentException("Session not found.");

        if (session.Status != ClassSessionStatus.Scheduled)
            throw new InvalidOperationException("Session is not open for enrollment.");

        var enrollmentCount = session.Enrollments.Count(e =>
            e.Status == ClassEnrollmentStatus.Pending ||
            e.Status == ClassEnrollmentStatus.Confirmed);

        if (enrollmentCount >= session.MaxStudents)
            throw new InvalidOperationException("Session is full.");

        var existing = session.Enrollments.FirstOrDefault(e => e.UserId == normalizedUserId);
        if (existing is not null && existing.Status != ClassEnrollmentStatus.Cancelled)
            throw new InvalidOperationException("User is already enrolled in this session.");

        var enrollment = new ClassEnrollment
        {
            ClassSessionId = sessionId,
            UserId = normalizedUserId,
            Status = ClassEnrollmentStatus.Pending,
            PaidAmount = 0m,
            EnrolledAt = DateTime.UtcNow
        };

        _db.ClassEnrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return ToDto(enrollment);
    }

    public async Task<bool> CancelEnrollmentAsync(int enrollmentId, string actorUserId, bool isAdmin)
    {
        var enrollment = await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId);
        if (enrollment is null) return false;

        var normalizedActor = actorUserId.Trim();
        if (!isAdmin && enrollment.UserId != normalizedActor)
            throw new UnauthorizedAccessException("You can only cancel your own enrollment.");

        enrollment.Status = ClassEnrollmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        return true;
    }

    private static void ValidateTimeRange(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("EndTime must be greater than StartTime.");
    }

    private static ClassSessionResponseDto ToDto(ClassSession session) => new(
        session.Id,
        session.RoomBookingId,
        session.TrainerId,
        session.Title,
        session.Description,
        session.MaxStudents,
        session.PricePerStudent,
        session.StartTime,
        session.EndTime,
        session.Status.ToString(),
        session.Enrollments?.Count(e => e.Status == ClassEnrollmentStatus.Confirmed) ?? 0
    );

    private static ClassEnrollmentResponseDto ToDto(ClassEnrollment enrollment) => new(
        enrollment.Id,
        enrollment.ClassSessionId,
        enrollment.UserId,
        enrollment.Status.ToString(),
        enrollment.PaidAmount,
        enrollment.StripePaymentIntentId,
        enrollment.EnrolledAt
    );
}
