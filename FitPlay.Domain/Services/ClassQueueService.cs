using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FitPlay.Domain.Services;

public class ClassQueueService
{
    private readonly FitPlayContext _db;

    public ClassQueueService(FitPlayContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Adds a user to the queue for a class whose trainer hasn't paid for the room yet.
    /// Members join for free; non-members pay 5% of the class price.
    /// Returns the queue entry info and (for non-members) a Stripe client secret placeholder.
    /// </summary>
    public async Task<(ClassQueueEntry entry, decimal queueCost, bool hasMembership)> JoinQueueAsync(int classScheduleId, int userId)
    {
        // Check if already in the queue
        var existing = await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == classScheduleId && q.UserId == userId);

        if (existing != null)
            throw new InvalidOperationException("You are already in the queue for this class.");

        var schedule = await _db.ClassSchedules
            .Include(s => s.RoomBooking)
            .FirstOrDefaultAsync(s => s.Id == classScheduleId)
            ?? throw new InvalidOperationException("Class not found.");

        if (schedule.Status != ClassScheduleStatus.Scheduled)
            throw new InvalidOperationException("This class is no longer available.");

        if (schedule.ScheduledAt <= DateTime.UtcNow)
            throw new InvalidOperationException("This class has already started.");

        // The class must have a linked room booking that is Pending (unpaid)
        if (schedule.RoomBookingId == null || schedule.RoomBooking == null)
            throw new InvalidOperationException("This class does not have an associated room booking.");

        if (schedule.RoomBooking.Status != RoomBookingStatus.Pending)
            throw new InvalidOperationException("The trainer has already paid for this class. Book normally instead.");

        // Check membership
        var hasMembership = await _db.Subscriptions
            .AnyAsync(s => s.ClientId == userId && s.Status == "Active");

        // Members who have skipped 5+ notified queue entries this calendar month
        // are treated as non-members for the queue cost (must pay the 5% deposit).
        bool memberExceedsSkipLimit = false;
        if (hasMembership)
        {
            var monthlySkips = await GetMonthlySkipCountAsync(userId);
            memberExceedsSkipLimit = monthlySkips >= 5;
        }

        // Calculate cost: free for members under skip limit, 5% for everyone else
        decimal queueCost = 0m;
        if (!hasMembership || memberExceedsSkipLimit)
        {
            var classPrice = ClassScheduleService.ExtractPriceFromNotes(schedule.Notes);
            if (classPrice.HasValue && classPrice.Value > 0)
            {
                queueCost = Math.Round(classPrice.Value * 0.05m, 2, MidpointRounding.AwayFromZero);
            }
        }

        var entry = new ClassQueueEntry
        {
            ClassScheduleId = classScheduleId,
            UserId = userId,
            HasMembership = hasMembership,
            QueueCost = queueCost,
            PaymentStatus = hasMembership ? ClassQueuePaymentStatus.None : ClassQueuePaymentStatus.None,
            CreatedAt = DateTime.UtcNow
        };

        _db.ClassQueueEntries.Add(entry);
        await _db.SaveChangesAsync();

        return (entry, queueCost, hasMembership);
    }

    /// <summary>
    /// Stores the Stripe PaymentIntent ID on the queue entry after it's been created externally.
    /// </summary>
    public async Task SetQueuePaymentIntentAsync(int queueEntryId, string paymentIntentId)
    {
        var entry = await _db.ClassQueueEntries.FindAsync(queueEntryId)
            ?? throw new InvalidOperationException("Queue entry not found.");

        entry.StripePaymentIntentId = paymentIntentId;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Confirms the queue payment for a non-member.
    /// </summary>
    public async Task ConfirmQueuePaymentAsync(int classScheduleId, int userId, string paymentIntentId)
    {
        var entry = await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == classScheduleId && q.UserId == userId)
            ?? throw new InvalidOperationException("Queue entry not found.");

        if (entry.PaymentStatus == ClassQueuePaymentStatus.Completed)
            return; // Idempotent

        entry.StripePaymentIntentId = paymentIntentId;
        entry.PaymentStatus = ClassQueuePaymentStatus.Completed;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Returns the number of users queued for a specific class schedule.
    /// </summary>
    public async Task<int> GetQueueCountAsync(int classScheduleId)
    {
        return await _db.ClassQueueEntries
            .CountAsync(q => q.ClassScheduleId == classScheduleId);
    }

    /// <summary>
    /// Returns queue counts for all class schedules linked to the given room booking IDs.
    /// Used by TrainerBookedRooms to display interested client counts per booking.
    /// </summary>
    public async Task<Dictionary<int, int>> GetQueueCountsByBookingIdsAsync(List<int> bookingIds)
    {
        if (!bookingIds.Any())
            return new Dictionary<int, int>();

        return await _db.ClassQueueEntries
            .Where(q => q.ClassSchedule != null && q.ClassSchedule.RoomBookingId != null
                        && bookingIds.Contains(q.ClassSchedule.RoomBookingId.Value))
            .GroupBy(q => q.ClassSchedule!.RoomBookingId!.Value)
            .Select(g => new { BookingId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookingId, x => x.Count);
    }

    /// <summary>
    /// Marks all queue entries as notified when the trainer pays for the room booking.
    /// Called from the booking payment confirmation flow.
    /// </summary>
    public async Task NotifyQueuedUsersAsync(int roomBookingId)
    {
        var entries = await _db.ClassQueueEntries
            .Where(q => q.ClassSchedule != null && q.ClassSchedule.RoomBookingId == roomBookingId && !q.IsNotified)
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.IsNotified = true;
        }

        if (entries.Any())
        {
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks whether a user is already in the queue for a given class schedule.
    /// </summary>
    public async Task<ClassQueueEntry?> GetUserQueueEntryAsync(int classScheduleId, int userId)
    {
        return await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == classScheduleId && q.UserId == userId);
    }

    /// <summary>
    /// Returns all class schedule IDs for which the user is queued.
    /// </summary>
    public async Task<List<int>> GetUserQueuedClassIdsAsync(int userId)
    {
        return await _db.ClassQueueEntries
            .Where(q => q.UserId == userId)
            .Select(q => q.ClassScheduleId)
            .ToListAsync();
    }

    /// <summary>
    /// Returns all queue entries for a user, including IsNotified and IsSkipped status.
    /// </summary>
    public async Task<List<UserQueueEntryDto>> GetUserQueueEntriesAsync(int userId)
    {
        return await _db.ClassQueueEntries
            .Where(q => q.UserId == userId)
            .Select(q => new UserQueueEntryDto(q.ClassScheduleId, q.IsNotified, q.QueueCost, q.SkippedAt != null))
            .ToListAsync();
    }

    /// <summary>
    /// Returns the number of times a member has skipped a notified queue entry
    /// in the current calendar month (UTC).
    /// </summary>
    public async Task<int> GetMonthlySkipCountAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.ClassQueueEntries
            .CountAsync(q => q.UserId == userId
                          && q.SkippedAt != null
                          && q.SkippedAt >= monthStart);
    }

    /// <summary>
    /// Marks a notified queue entry as skipped ("Not interested").
    /// The entry is kept so the skip is counted toward the monthly limit.
    /// </summary>
    public async Task SkipQueueEntryAsync(int classScheduleId, int userId)
    {
        var entry = await _db.ClassQueueEntries
            .FirstOrDefaultAsync(q => q.ClassScheduleId == classScheduleId && q.UserId == userId)
            ?? throw new InvalidOperationException("Queue entry not found.");

        if (!entry.IsNotified)
            throw new InvalidOperationException("This class has not been notified yet.");

        if (entry.SkippedAt != null)
            return; // Idempotent — already skipped

        entry.SkippedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
