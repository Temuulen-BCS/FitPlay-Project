using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public enum ClassQueuePaymentStatus
{
    None,
    Completed
}

public class ClassQueueEntry
{
    public int Id { get; set; }
    public int ClassScheduleId { get; set; }
    public int UserId { get; set; }
    public bool HasMembership { get; set; }

    /// <summary>
    /// 0 for members, 5% of class price for non-members.
    /// </summary>
    public decimal QueueCost { get; set; }

    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }
    public ClassQueuePaymentStatus PaymentStatus { get; set; } = ClassQueuePaymentStatus.None;

    /// <summary>
    /// Set to true when the trainer pays for the room booking,
    /// signalling this student should complete full payment.
    /// </summary>
    public bool IsNotified { get; set; }

    /// <summary>
    /// Set when the user dismisses a notification without booking ("Not interested").
    /// Used to enforce the 5-skip-per-month limit for members.
    /// </summary>
    public DateTime? SkippedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClassSchedule? ClassSchedule { get; set; }
}
