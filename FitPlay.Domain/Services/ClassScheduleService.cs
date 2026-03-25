using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FitPlay.Domain.Services;

public class ClassScheduleService
{
    private readonly FitPlayContext _db;

    public ClassScheduleService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<List<ClassScheduleDto>> GetUserScheduleAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.ClassSchedules
            .Where(s => s.UserId == userId)
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

    public async Task<List<ClassScheduleWithTrainerDto>> GetUserScheduleWithTrainerAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.ClassSchedules
            .Where(s => s.UserId == userId)
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
            .Include(s => s.Trainer)
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
                        s.ScheduledAt > DateTime.UtcNow &&
                        (s.UserId == null || s.UserId == 0))
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.ScheduledAt >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.ScheduledAt <= to.Value);

        var items = await query
            .Include(s => s.Trainer)
            .Include(s => s.RoomBooking)
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
    /// Books a free class (no payment required). Returns null if already booked.
    /// </summary>
    public async Task<ClassScheduleDto?> BookClassAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules.FindAsync(scheduleId);
        if (schedule == null) return null;

        if (schedule.UserId.HasValue && schedule.UserId.Value != 0)
            return null;

        schedule.UserId = userId;
        schedule.Status = ClassScheduleStatus.Scheduled;

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
    /// Validates a schedule is bookable (exists, available, has a price) and returns the price.
    /// Throws descriptive exceptions on failure.
    /// </summary>
    public async Task<(ClassSchedule schedule, decimal price)> ValidateForPaymentAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules
            .Include(s => s.Trainer)
            .FirstOrDefaultAsync(s => s.Id == scheduleId)
            ?? throw new InvalidOperationException("Class not found.");

        if (schedule.Status != ClassScheduleStatus.Scheduled)
            throw new InvalidOperationException("This class is no longer available.");

        if (schedule.ScheduledAt <= DateTime.UtcNow)
            throw new InvalidOperationException("This class has already started.");

        if (schedule.UserId.HasValue && schedule.UserId.Value != 0)
            throw new InvalidOperationException("This class is already booked.");

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
    /// Confirms a paid booking: assigns user, marks payment as completed.
    /// </summary>
    public async Task<ClassScheduleDto?> ConfirmPaidBookingAsync(int scheduleId, int userId, string paymentIntentId, decimal paidAmount)
    {
        var schedule = await _db.ClassSchedules.FindAsync(scheduleId);
        if (schedule == null) return null;

        // Idempotency: if already confirmed by this PI, return as-is
        if (schedule.PaymentStatus == ClassSchedulePaymentStatus.Completed &&
            schedule.StripePaymentIntentId == paymentIntentId)
        {
            return ToDto(schedule);
        }

        schedule.UserId = userId;
        schedule.Status = ClassScheduleStatus.Scheduled;
        schedule.StripePaymentIntentId = paymentIntentId;
        schedule.PaymentStatus = ClassSchedulePaymentStatus.Completed;
        schedule.PaidAmount = paidAmount;
        schedule.PaidAt = DateTime.UtcNow;

        // Remove any queue entry for this user + class (converts queue → booking)
        var queueEntry = await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == scheduleId && q.UserId == userId);
        if (queueEntry != null)
            _db.ClassQueueEntries.Remove(queueEntry);

        await _db.SaveChangesAsync();

        return ToDto(schedule);
    }

    /// <summary>
    /// Unbooks a free class (no payment tracking needed).
    /// </summary>
    public async Task<ClassScheduleDto?> UnbookFreeAsync(int id)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.UserId = null;
        schedule.Status = ClassScheduleStatus.Scheduled;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// Marks a paid booking as refunded and removes the user assignment.
    /// Called after Stripe refund has been issued successfully.
    /// </summary>
    public async Task<ClassScheduleDto?> MarkRefundedAsync(int id)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.UserId = null;
        schedule.Status = ClassScheduleStatus.Scheduled;
        schedule.PaymentStatus = ClassSchedulePaymentStatus.Refunded;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    // ── Helpers ──

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
        return new ClassScheduleDto(
            Id: schedule.Id,
            UserId: schedule.UserId,
            TrainerId: schedule.TrainerId,
            Modality: schedule.Modality,
            ScheduledAt: schedule.ScheduledAt,
            Status: schedule.Status.ToString(),
            Notes: schedule.Notes,
            PaymentStatus: schedule.PaymentStatus.ToString(),
            PaidAmount: schedule.PaidAmount
        );
    }

    private static ClassScheduleWithTrainerDto ToWithTrainerDto(ClassSchedule s, int queueCount = 0)
    {
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
            QueueCount: queueCount
        );
    }

    private static ClassScheduleStatus ParseStatus(string status)
    {
        return Enum.TryParse<ClassScheduleStatus>(status, true, out var parsed)
            ? parsed
            : ClassScheduleStatus.Scheduled;
    }
}
