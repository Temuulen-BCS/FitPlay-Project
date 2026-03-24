using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public enum ClassScheduleStatus
{
    Scheduled,
    Cancelled,
    Completed
}

public enum ClassSchedulePaymentStatus
{
    None,
    Pending,
    Completed,
    Failed,
    Refunded
}

public class ClassSchedule
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? TrainerId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Modality { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }
    public ClassScheduleStatus Status { get; set; } = ClassScheduleStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Payment fields
    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }
    public ClassSchedulePaymentStatus PaymentStatus { get; set; } = ClassSchedulePaymentStatus.None;
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }

    public User? User { get; set; }
    public Teacher? Trainer { get; set; }
}
