using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FitPlay.Domain.Services;

public class ClassScheduleService
{
    private readonly FitPlayContext _db;
    private readonly IClockService _clock;

    public ClassScheduleService(FitPlayContext db, IClockService clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<ClassScheduleDto>> GetUserScheduleAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        // Get user's IdentityUserId
        var domainUser = await _db.Users.FindAsync(userId);
        if (domainUser?.IdentityUserId == null) return new List<ClassScheduleDto>();

        var identityUserId = domainUser.IdentityUserId.Trim();

        // Query schedules via enrollments
        var query = from e in _db.ClassEnrollments
                    join s in _db.ClassSessions on e.ClassSessionId equals s.Id
                    join rb in _db.RoomBookings on s.RoomBookingId equals rb.Id
                    join cs in _db.ClassSchedules on rb.Id equals cs.RoomBookingId
                    where e.UserId == identityUserId
                          && e.Status != ClassEnrollmentStatus.Cancelled
                    select cs;

        if (from.HasValue)
        {
            query = query.Where(s => s.ScheduledAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.ScheduledAt <= to.Value);
        }

        var items = await query
            .Distinct()
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<List<ClassScheduleWithTrainerDto>> GetUserScheduleWithTrainerAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        // Get user's IdentityUserId
        var domainUser = await _db.Users.FindAsync(userId);
        if (domainUser?.IdentityUserId == null) return new List<ClassScheduleWithTrainerDto>();

        var identityUserId = domainUser.IdentityUserId.Trim();

        // Query schedules via enrollments
        var query = from e in _db.ClassEnrollments
                    join s in _db.ClassSessions on e.ClassSessionId equals s.Id
                    join rb in _db.RoomBookings on s.RoomBookingId equals rb.Id
                    join cs in _db.ClassSchedules on rb.Id equals cs.RoomBookingId
                    where e.UserId == identityUserId
                          && e.Status != ClassEnrollmentStatus.Cancelled
                    select cs;

        if (from.HasValue)
        {
            query = query.Where(s => s.ScheduledAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.ScheduledAt <= to.Value);
        }

        var items = await query
            .Include(s => s.Trainer)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.Room)
                    .ThenInclude(r => r!.GymLocation)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .Distinct()
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return items.Select(ToWithTrainerDto).ToList();
    }

    public async Task<ClassScheduleDto?> GetByIdAsync(int id)
    {
        var schedule = await _db.ClassSchedules
            .FirstOrDefaultAsync(s => s.Id == id);

        return schedule == null ? null : ToDto(schedule);
    }

    public async Task<ClassSchedule?> GetEntityByIdAsync(int id)
        => await _db.ClassSchedules.Include(s => s.Trainer).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<ClassScheduleDto> CreateAsync(CreateClassScheduleRequest request)
    {
        var schedule = new ClassSchedule
        {
            UserId = request.UserId.HasValue && request.UserId.Value > 0 ? request.UserId : null,
            TrainerId = request.TrainerId.HasValue && request.TrainerId.Value > 0 ? request.TrainerId : null,
            Modality = request.Modality.Trim(),
            ScheduledAt = request.ScheduledAt,
            Notes = request.Notes,
            RoomBookingId = request.RoomBookingId,
            Status = ClassScheduleStatus.Scheduled
        };

        _db.ClassSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(schedule.Id) ?? ToDto(schedule);
    }

    public async Task<ClassScheduleDto?> UpdateAsync(int id, UpdateClassScheduleRequest request)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.Modality = request.Modality.Trim();
        schedule.ScheduledAt = request.ScheduledAt;
        schedule.Notes = request.Notes;
        schedule.Status = ParseStatus(request.Status);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return false;

        _db.ClassSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ClassScheduleWithTrainerDto>> GetPublicSchedulesAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.ClassSchedules
            .Where(s => s.Status == ClassScheduleStatus.Scheduled &&
                         s.ScheduledAt > _clock.UtcNow)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.ScheduledAt >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.ScheduledAt <= to.Value);

        var items = await query
            .Include(s => s.Trainer)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.Room)
                    .ThenInclude(r => r!.GymLocation)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        // Batch-load queue counts for all schedule IDs
        var scheduleIds = items.Select(s => s.Id).ToList();
        var queueCounts = await _db.ClassQueueEntries
            .Where(q => scheduleIds.Contains(q.ClassScheduleId))
            .GroupBy(q => q.ClassScheduleId)
            .Select(g => new { ClassScheduleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClassScheduleId, x => x.Count);

        return items.Select(s => ToWithTrainerDto(s, queueCounts.GetValueOrDefault(s.Id, 0))).ToList();
    }

    public async Task<List<ClassScheduleDto>> GetTrainerScheduleAsync(int trainerId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.ClassSchedules
            .Where(s => s.TrainerId == trainerId)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(s => s.ScheduledAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.ScheduledAt <= to.Value);
        }

        var items = await query
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    /// <summary>
    /// Books a free class (no payment required) by creating a ClassEnrollment.
    /// Throws if class is full or user already enrolled.
    /// </summary>
    public async Task<ClassScheduleDto?> BookClassAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.Room)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null) return null;

        // Ensure ClassSession exists (auto-create if missing)
        var session = await EnsureClassSessionAsync(schedule);

        // Get user's IdentityUserId for the enrollment
        var domainUser = await _db.Users.FindAsync(userId);
        if (domainUser?.IdentityUserId == null)
            throw new InvalidOperationException("User not found or has no identity link.");

        var identityUserId = domainUser.IdentityUserId.Trim();

        // Check capacity
        var enrollmentCount = session.Enrollments.Count(e =>
            e.Status == ClassEnrollmentStatus.Pending ||
            e.Status == ClassEnrollmentStatus.Confirmed);

        if (enrollmentCount >= session.MaxStudents)
            throw new InvalidOperationException("This class is full.");

        // Check existing enrollment
        var existing = session.Enrollments.FirstOrDefault(e => e.UserId == identityUserId);
        if (existing is not null)
        {
            if (existing.Status != ClassEnrollmentStatus.Cancelled)
                throw new InvalidOperationException("You are already enrolled in this class.");

            // Reuse cancelled enrollment to avoid unique constraint violation
            existing.Status = ClassEnrollmentStatus.Confirmed;
            existing.PaidAmount = 0m;
            existing.StripePaymentIntentId = null;
            existing.EnrolledAt = _clock.UtcNow;
        }
        else
        {
            // Create enrollment
            var enrollment = new ClassEnrollment
            {
                ClassSessionId = session.Id,
                UserId = identityUserId,
                Status = ClassEnrollmentStatus.Confirmed,
                PaidAmount = 0m,
                EnrolledAt = _clock.UtcNow
            };

            _db.ClassEnrollments.Add(enrollment);
        }

        // Remove any queue entry for this user + class (converts queue → booking)
        var queueEntry = await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == scheduleId && q.UserId == userId);
        if (queueEntry != null)
            _db.ClassQueueEntries.Remove(queueEntry);

        await _db.SaveChangesAsync();

        return ToDto(schedule);
    }

    public async Task<ClassScheduleDto?> UpdateStatusAsync(int id, string status)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.Status = ParseStatus(status);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    // ── Payment methods ──

    /// <summary>
    /// Validates a schedule is bookable (exists, available, has a price, has capacity) and returns the price.
    /// Throws descriptive exceptions on failure.
    /// </summary>
    public async Task<(ClassSchedule schedule, decimal price)> ValidateForPaymentAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules
            .Include(s => s.Trainer)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.Room)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId)
            ?? throw new InvalidOperationException("Class not found.");

        if (schedule.Status != ClassScheduleStatus.Scheduled)
            throw new InvalidOperationException("This class is no longer available.");

        if (schedule.ScheduledAt <= _clock.UtcNow)
            throw new InvalidOperationException("This class has already started.");

        // Ensure ClassSession exists (auto-create if missing)
        var session = await EnsureClassSessionAsync(schedule);

        // Check capacity
        var enrollmentCount = session.Enrollments.Count(e =>
            e.Status == ClassEnrollmentStatus.Pending ||
            e.Status == ClassEnrollmentStatus.Confirmed);

        if (enrollmentCount >= session.MaxStudents)
            throw new InvalidOperationException("This class is full.");

        // Check if user already enrolled
        var domainUser = await _db.Users.FindAsync(userId);
        if (domainUser?.IdentityUserId != null)
        {
            var identityUserId = domainUser.IdentityUserId.Trim();
            var existing = session.Enrollments.FirstOrDefault(e => e.UserId == identityUserId);
            if (existing is not null && existing.Status != ClassEnrollmentStatus.Cancelled)
                throw new InvalidOperationException("You are already enrolled in this class.");
        }

        var price = ExtractPriceFromNotes(schedule.Notes)
            ?? throw new InvalidOperationException("This class has no price set. Use the free booking endpoint.");

        if (price <= 0)
            throw new InvalidOperationException("Invalid class price.");

        return (schedule, price);
    }

    /// <summary>
    /// Stores a pending PaymentIntent on the schedule so it can be reused/verified later.
    /// </summary>
    public async Task SetPaymentIntentAsync(int scheduleId, string paymentIntentId)
    {
        var schedule = await _db.ClassSchedules.FindAsync(scheduleId)
            ?? throw new InvalidOperationException("Class not found.");

        schedule.StripePaymentIntentId = paymentIntentId;
        schedule.PaymentStatus = ClassSchedulePaymentStatus.Pending;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Confirms a paid booking: creates ClassEnrollment, marks payment as completed.
    /// </summary>
    public async Task<ClassScheduleDto?> ConfirmPaidBookingAsync(int scheduleId, int userId, string paymentIntentId, decimal paidAmount)
    {
        var schedule = await _db.ClassSchedules
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.Room)
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null) return null;

        // Ensure ClassSession exists (auto-create if missing)
        var session = await EnsureClassSessionAsync(schedule);

        // Get user's IdentityUserId
        var domainUser = await _db.Users.FindAsync(userId);
        if (domainUser?.IdentityUserId == null)
            throw new InvalidOperationException("User not found or has no identity link.");

        var identityUserId = domainUser.IdentityUserId.Trim();

        // Idempotency: if already enrolled via this payment intent, return as-is
        var existingEnrollment = session.Enrollments.FirstOrDefault(e =>
            e.UserId == identityUserId &&
            e.StripePaymentIntentId == paymentIntentId &&
            e.Status == ClassEnrollmentStatus.Confirmed);

        if (existingEnrollment != null)
        {
            return ToDto(schedule);
        }

        // Check capacity
        var enrollmentCount = session.Enrollments.Count(e =>
            e.Status == ClassEnrollmentStatus.Pending ||
            e.Status == ClassEnrollmentStatus.Confirmed);

        if (enrollmentCount >= session.MaxStudents)
            throw new InvalidOperationException("This class is full.");

        // Check existing enrollment
        var existing = session.Enrollments.FirstOrDefault(e => e.UserId == identityUserId);
        if (existing is not null)
        {
            if (existing.Status != ClassEnrollmentStatus.Cancelled)
                throw new InvalidOperationException("You are already enrolled in this class.");

            // Reuse cancelled enrollment to avoid unique constraint violation
            existing.Status = ClassEnrollmentStatus.Confirmed;
            existing.PaidAmount = paidAmount;
            existing.StripePaymentIntentId = paymentIntentId;
            existing.EnrolledAt = _clock.UtcNow;
        }
        else
        {
            // Create enrollment
            var enrollment = new ClassEnrollment
            {
                ClassSessionId = session.Id,
                UserId = identityUserId,
                Status = ClassEnrollmentStatus.Confirmed,
                PaidAmount = paidAmount,
                StripePaymentIntentId = paymentIntentId,
                EnrolledAt = _clock.UtcNow
            };

            _db.ClassEnrollments.Add(enrollment);
        }

        // Mark payment as completed on the schedule
        schedule.StripePaymentIntentId = paymentIntentId;
        schedule.PaymentStatus = ClassSchedulePaymentStatus.Completed;
        schedule.PaidAmount = paidAmount;
        schedule.PaidAt = _clock.UtcNow;

        // Remove any queue entry for this user + class (converts queue → booking)
        var queueEntry = await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == scheduleId && q.UserId == userId);
        if (queueEntry != null)
            _db.ClassQueueEntries.Remove(queueEntry);

        await _db.SaveChangesAsync();

        return ToDto(schedule);
    }

    /// <summary>
    /// Cancels a user's enrollment (free class, no payment tracking needed).
    /// </summary>
    public async Task<ClassScheduleDto?> UnbookFreeAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null) return null;

        var session = schedule.RoomBooking?.ClassSession;
        if (session == null) return null;

        // Get user's IdentityUserId
        var domainUser = await _db.Users.FindAsync(userId);
        if (domainUser?.IdentityUserId == null) return null;

        var identityUserId = domainUser.IdentityUserId.Trim();

        var enrollment = session.Enrollments.FirstOrDefault(e =>
            e.UserId == identityUserId &&
            e.Status != ClassEnrollmentStatus.Cancelled);

        if (enrollment != null)
        {
            enrollment.Status = ClassEnrollmentStatus.Cancelled;
            await _db.SaveChangesAsync();
        }

        return await GetByIdAsync(scheduleId);
    }

    /// <summary>
    /// Marks a paid booking as refunded and cancels the user's enrollment.
    /// Called after Stripe refund has been issued successfully.
    /// </summary>
    public async Task<ClassScheduleDto?> MarkRefundedAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules
            .Include(s => s.RoomBooking)
                .ThenInclude(rb => rb!.ClassSession)
                    .ThenInclude(cs => cs!.Enrollments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null) return null;

        var session = schedule.RoomBooking?.ClassSession;
        if (session != null)
        {
            // Get user's IdentityUserId
            var domainUser = await _db.Users.FindAsync(userId);
            if (domainUser?.IdentityUserId != null)
            {
                var identityUserId = domainUser.IdentityUserId.Trim();
                var enrollment = session.Enrollments.FirstOrDefault(e =>
                    e.UserId == identityUserId &&
                    e.Status != ClassEnrollmentStatus.Cancelled);

                if (enrollment != null)
                {
                    enrollment.Status = ClassEnrollmentStatus.Cancelled;
                }
            }
        }

        schedule.PaymentStatus = ClassSchedulePaymentStatus.Refunded;
        schedule.StripePaymentIntentId = null;
        schedule.PaidAmount = null;
        schedule.PaidAt = null;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(scheduleId);
    }

    // ── Helpers ──

    /// <summary>
    /// Ensures a ClassSession exists for the given RoomBooking. If not, creates one automatically.
    /// Returns the session (existing or newly created).
    /// </summary>
    private async Task<ClassSession> EnsureClassSessionAsync(ClassSchedule schedule)
    {
        if (schedule.RoomBooking == null)
            throw new InvalidOperationException("This class has no linked room booking.");

        var roomBooking = schedule.RoomBooking;

        // If session already exists, return it
        if (roomBooking.ClassSession != null)
            return roomBooking.ClassSession;

        // Auto-create a ClassSession based on RoomBooking and ClassSchedule info
        var session = new ClassSession
        {
            RoomBookingId = roomBooking.Id,
            TrainerId = roomBooking.TrainerId,
            Title = $"{schedule.Modality} Class",
            Description = schedule.Notes,
            MaxStudents = roomBooking.Room?.Capacity ?? 20, // Default to 20 if room capacity not available
            PricePerStudent = ExtractPriceFromNotes(schedule.Notes) ?? 0m,
            StartTime = roomBooking.StartTime,
            EndTime = roomBooking.EndTime,
            Status = ClassSessionStatus.Scheduled
        };

        _db.ClassSessions.Add(session);
        await _db.SaveChangesAsync();

        // Update the navigation property so it's available immediately
        roomBooking.ClassSession = session;

        return session;
    }

    public static decimal? ExtractPriceFromNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;

        var parts = notes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            const string marker = "PricePerStudent=";
            if (!part.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = part[marker.Length..].Trim();
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
                return parsed;
        }

        return null;
    }

    private static ClassScheduleDto ToDto(ClassSchedule schedule)
    {
        // Calculate duration from RoomBooking if available
        double? durationMinutes = null;
        if (schedule.RoomBooking != null)
        {
            var duration = schedule.RoomBooking.EndTime - schedule.RoomBooking.StartTime;
            durationMinutes = duration.TotalMinutes;
        }

        // Capacity from ClassSession, falling back to Room.Capacity for schedules without a session
        int? maxCapacity = schedule.RoomBooking?.ClassSession?.MaxStudents
                           ?? schedule.RoomBooking?.Room?.Capacity;
        int? bookedCount = schedule.RoomBooking?.ClassSession?.Enrollments?
            .Count(e => e.Status != ClassEnrollmentStatus.Cancelled);

        // Location info
        string? roomName = schedule.RoomBooking?.Room?.Name;
        var gymLocation = schedule.RoomBooking?.Room?.GymLocation;
        string? gymLocationName = gymLocation?.Name;
        string? gymLocationAddress = gymLocation?.Address;
        string? gymLocationCity = gymLocation?.City;
        string? gymLocationState = gymLocation?.State;
        string? gymLocationZipCode = gymLocation?.ZipCode;

        return new ClassScheduleDto(
            Id: schedule.Id,
            UserId: schedule.UserId,
            TrainerId: schedule.TrainerId,
            Modality: schedule.Modality,
            ScheduledAt: schedule.ScheduledAt,
            Status: schedule.Status.ToString(),
            Notes: schedule.Notes,
            PaymentStatus: schedule.PaymentStatus.ToString(),
            PaidAmount: schedule.PaidAmount,
            DurationMinutes: durationMinutes,
            MaxCapacity: maxCapacity,
            BookedCount: bookedCount,
            RoomName: roomName,
            GymLocationName: gymLocationName,
            GymLocationAddress: gymLocationAddress,
            GymLocationCity: gymLocationCity,
            GymLocationState: gymLocationState,
            GymLocationZipCode: gymLocationZipCode
        );
    }

    private static ClassScheduleWithTrainerDto ToWithTrainerDto(ClassSchedule s, int queueCount = 0)
    {
        // Calculate duration from RoomBooking if available
        double? durationMinutes = null;
        if (s.RoomBooking != null)
        {
            var duration = s.RoomBooking.EndTime - s.RoomBooking.StartTime;
            durationMinutes = duration.TotalMinutes;
        }

        // Capacity from ClassSession, falling back to Room.Capacity for schedules without a session
        int? maxCapacity = s.RoomBooking?.ClassSession?.MaxStudents
                           ?? s.RoomBooking?.Room?.Capacity;
        int? bookedCount = s.RoomBooking?.ClassSession?.Enrollments?
            .Count(e => e.Status != ClassEnrollmentStatus.Cancelled);

        // Location info
        string? roomName = s.RoomBooking?.Room?.Name;
        var gymLocation = s.RoomBooking?.Room?.GymLocation;
        string? gymLocationName = gymLocation?.Name;
        string? gymLocationAddress = gymLocation?.Address;
        string? gymLocationCity = gymLocation?.City;
        string? gymLocationState = gymLocation?.State;
        string? gymLocationZipCode = gymLocation?.ZipCode;

        return new ClassScheduleWithTrainerDto(
            s.Id,
            s.TrainerId,
            s.Trainer?.Name ?? "TBA",
            s.Modality,
            s.ScheduledAt,
            s.Status.ToString(),
            s.Notes,
            s.PaymentStatus.ToString(),
            s.PaidAmount,
            RoomBookingStatus: s.RoomBooking?.Status.ToString(),
            QueueCount: queueCount,
            DurationMinutes: durationMinutes,
            MaxCapacity: maxCapacity,
            BookedCount: bookedCount,
            IsBooked: maxCapacity.HasValue && bookedCount.HasValue && bookedCount.Value >= maxCapacity.Value,
            RoomName: roomName,
            GymLocationName: gymLocationName,
            GymLocationAddress: gymLocationAddress,
            GymLocationCity: gymLocationCity,
            GymLocationState: gymLocationState,
            GymLocationZipCode: gymLocationZipCode
        );
    }

    private static ClassScheduleStatus ParseStatus(string status)
    {
        return Enum.TryParse<ClassScheduleStatus>(status, true, out var parsed)
            ? parsed
            : ClassScheduleStatus.Scheduled;
    }
}
