namespace FitPlay.Domain.Models;

public enum ClassEnrollmentStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public class ClassEnrollment
{
    public int Id { get; set; }
    public int ClassSessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ClassEnrollmentStatus Status { get; set; } = ClassEnrollmentStatus.Pending;
    public decimal PaidAmount { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public ClassSession? ClassSession { get; set; }
    public PaymentSplit? PaymentSplit { get; set; }
    public List<RoomCheckIn> CheckIns { get; set; } = new();
}
