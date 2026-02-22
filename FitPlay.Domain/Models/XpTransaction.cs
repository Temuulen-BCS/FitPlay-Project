namespace FitPlay.Domain.Models;

/// <summary>
/// Types of XP transactions in the gamification system.
/// </summary>
public enum XpTransactionType
{
    TrainingCompletion,
    BonusFromTrainer,
    Reset,
    Adjustment
}

/// <summary>
/// Audit log for all XP changes (awards, bonuses, resets).
/// </summary>
public class XpTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public XpTransactionType TransactionType { get; set; }
    public int? SourceId { get; set; } // TrainingCompletion Id, etc.
    public int XpDelta { get; set; }
    public int XpBefore { get; set; }
    public int XpAfter { get; set; }
    public string? Reason { get; set; }
    public int? AwardedByTrainerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Teacher? AwardedByTrainer { get; set; }
}
