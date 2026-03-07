namespace FitPlay.Domain.Models;

public class PaymentSplit
{
    public int Id { get; set; }
    public int ClassEnrollmentId { get; set; }
    public decimal GymAmount { get; set; }
    public decimal TrainerAmount { get; set; }
    public decimal PlatformAmount { get; set; }
    public string? StripeTransferId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    public ClassEnrollment? ClassEnrollment { get; set; }
}
