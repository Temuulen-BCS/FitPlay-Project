namespace FitPlay.Domain.Models;

/// <summary>
/// Tracks a user's current level and total XP in the gamification system.
/// </summary>
public class UserLevel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CurrentLevel { get; set; } = 1;
    public int TotalXp { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
