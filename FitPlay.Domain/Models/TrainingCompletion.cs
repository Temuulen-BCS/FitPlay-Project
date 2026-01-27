namespace FitPlay.Domain.Models;

/// <summary>
/// Validation status for a training completion.
/// </summary>
public enum ValidationStatus
{
    AutoApproved,
    Pending,
    Validated,
    Rejected
}

/// <summary>
/// Records a completed training by a user, including XP awarded.
/// </summary>
public class TrainingCompletion
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public int UserId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public int XpGranted { get; set; }
    public ValidationStatus Status { get; set; } = ValidationStatus.AutoApproved;
    public int? ValidatedByTrainerId { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Training? Training { get; set; }
    public User? User { get; set; }
    public Teacher? ValidatedByTrainer { get; set; }
}
