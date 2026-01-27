namespace FitPlay.Domain.Models;

/// <summary>
/// Defines the XP thresholds for each level in the gamification system.
/// </summary>
public class LevelDefinition
{
    public int Id { get; set; }
    public int Level { get; set; }
    public int MinXp { get; set; }
    public int MaxXp { get; set; }
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Default level thresholds as per spec.
    /// </summary>
    public static readonly LevelDefinition[] DefaultLevels = new[]
    {
        new LevelDefinition { Level = 1, MinXp = 0, MaxXp = 499, Label = "Beginner" },
        new LevelDefinition { Level = 2, MinXp = 500, MaxXp = 1499, Label = "Novice" },
        new LevelDefinition { Level = 3, MinXp = 1500, MaxXp = 2999, Label = "Intermediate" },
        new LevelDefinition { Level = 4, MinXp = 3000, MaxXp = 4999, Label = "Advanced" },
        new LevelDefinition { Level = 5, MinXp = 5000, MaxXp = 7499, Label = "Expert" },
        new LevelDefinition { Level = 6, MinXp = 7500, MaxXp = 10499, Label = "Master" },
        new LevelDefinition { Level = 7, MinXp = 10500, MaxXp = 14999, Label = "Elite" },
        new LevelDefinition { Level = 8, MinXp = 15000, MaxXp = 19999, Label = "Champion" },
        new LevelDefinition { Level = 9, MinXp = 20000, MaxXp = 29999, Label = "Legend" },
        new LevelDefinition { Level = 10, MinXp = 30000, MaxXp = int.MaxValue, Label = "Mythic" },
    };

    /// <summary>
    /// Calculate level from total XP.
    /// </summary>
    public static int GetLevelFromXp(int totalXp)
    {
        for (int i = DefaultLevels.Length - 1; i >= 0; i--)
        {
            if (totalXp >= DefaultLevels[i].MinXp)
                return DefaultLevels[i].Level;
        }
        return 1;
    }

    /// <summary>
    /// Get the XP required for the next level.
    /// </summary>
    public static int GetNextLevelXp(int currentLevel)
    {
        if (currentLevel >= DefaultLevels.Length)
            return int.MaxValue;
        return DefaultLevels[currentLevel].MinXp;
    }

    /// <summary>
    /// Get the label for a level.
    /// </summary>
    public static string GetLevelLabel(int level)
    {
        var def = DefaultLevels.FirstOrDefault(l => l.Level == level);
        return def?.Label ?? "Unknown";
    }
}
