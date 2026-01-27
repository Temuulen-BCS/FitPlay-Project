namespace FitPlay.Domain.Models;

/// <summary>
/// Achievement/badge earned by a user.
/// </summary>
public class Achievement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AchievementType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

/// <summary>
/// Defines available achievement types and their conditions.
/// </summary>
public static class AchievementTypes
{
    public const string FirstTraining = "first_training";
    public const string SevenDayStreak = "7_day_streak";
    public const string ThirtyDayStreak = "30_day_streak";
    public const string TenTrainings = "10_trainings";
    public const string FiftyTrainings = "50_trainings";
    public const string HundredTrainings = "100_trainings";
    public const string LevelUp = "level_up";
    public const string Level5 = "level_5";
    public const string Level10 = "level_10";

    public static readonly Dictionary<string, (string Name, string Description)> Definitions = new()
    {
        { FirstTraining, ("First Steps", "Complete your first training") },
        { SevenDayStreak, ("Week Warrior", "Complete trainings 7 days in a row") },
        { ThirtyDayStreak, ("Monthly Master", "Complete trainings 30 days in a row") },
        { TenTrainings, ("Getting Started", "Complete 10 trainings") },
        { FiftyTrainings, ("Dedicated", "Complete 50 trainings") },
        { HundredTrainings, ("Centurion", "Complete 100 trainings") },
        { LevelUp, ("Level Up!", "Reach a new level") },
        { Level5, ("Expert Tier", "Reach level 5") },
        { Level10, ("Mythic Status", "Reach level 10") },
    };
}
