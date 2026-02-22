using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

/// <summary>
/// Service for handling achievements/badges.
/// </summary>
public class AchievementService
{
    private readonly FitPlayContext _db;

    public AchievementService(FitPlayContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Check and award any achievements the user has earned.
    /// </summary>
    public async Task<List<AchievementDto>> CheckAndAwardAchievementsAsync(int userId, int currentLevel, bool justLeveledUp)
    {
        var newAchievements = new List<Achievement>();
        var existingTypes = await _db.Achievements
            .Where(a => a.UserId == userId)
            .Select(a => a.AchievementType)
            .ToListAsync();

        var completionsCount = await _db.TrainingCompletions
            .CountAsync(c => c.UserId == userId && 
                (c.Status == ValidationStatus.AutoApproved || c.Status == ValidationStatus.Validated));

        var streak = await CalculateStreakAsync(userId);

        // First training
        if (completionsCount == 1 && !existingTypes.Contains(AchievementTypes.FirstTraining))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.FirstTraining));
        }

        // Training count milestones
        if (completionsCount >= 10 && !existingTypes.Contains(AchievementTypes.TenTrainings))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.TenTrainings));
        }
        if (completionsCount >= 50 && !existingTypes.Contains(AchievementTypes.FiftyTrainings))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.FiftyTrainings));
        }
        if (completionsCount >= 100 && !existingTypes.Contains(AchievementTypes.HundredTrainings))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.HundredTrainings));
        }

        // Streak achievements
        if (streak >= 7 && !existingTypes.Contains(AchievementTypes.SevenDayStreak))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.SevenDayStreak));
        }
        if (streak >= 30 && !existingTypes.Contains(AchievementTypes.ThirtyDayStreak))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.ThirtyDayStreak));
        }

        // Level achievements
        if (justLeveledUp && !existingTypes.Contains(AchievementTypes.LevelUp))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.LevelUp));
        }
        if (currentLevel >= 5 && !existingTypes.Contains(AchievementTypes.Level5))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.Level5));
        }
        if (currentLevel >= 10 && !existingTypes.Contains(AchievementTypes.Level10))
        {
            newAchievements.Add(CreateAchievement(userId, AchievementTypes.Level10));
        }

        if (newAchievements.Any())
        {
            _db.Achievements.AddRange(newAchievements);
            await _db.SaveChangesAsync();
        }

        return newAchievements.Select(ToDto).ToList();
    }

    /// <summary>
    /// Get all achievements for a user.
    /// </summary>
    public async Task<List<AchievementDto>> GetUserAchievementsAsync(int userId)
    {
        var achievements = await _db.Achievements
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AwardedAt)
            .ToListAsync();

        return achievements.Select(ToDto).ToList();
    }

    /// <summary>
    /// Get all available achievement types with earned status.
    /// </summary>
    public async Task<List<(AchievementDto? Earned, string Type, string Name, string Description)>> GetAllAchievementsStatusAsync(int userId)
    {
        var earned = await _db.Achievements
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.AchievementType);

        return AchievementTypes.Definitions.Select(def =>
        {
            earned.TryGetValue(def.Key, out var achievement);
            return (
                Earned: achievement != null ? ToDto(achievement) : null,
                Type: def.Key,
                Name: def.Value.Name,
                Description: def.Value.Description
            );
        }).ToList();
    }

    private Achievement CreateAchievement(int userId, string achievementType)
    {
        var def = AchievementTypes.Definitions[achievementType];
        return new Achievement
        {
            UserId = userId,
            AchievementType = achievementType,
            Name = def.Name,
            Description = def.Description
        };
    }

    private AchievementDto ToDto(Achievement a) => new(
        a.Id,
        a.AchievementType,
        a.Name,
        a.Description,
        a.IconUrl,
        a.AwardedAt
    );

    private async Task<int> CalculateStreakAsync(int userId)
    {
        var completions = await _db.TrainingCompletions
            .Where(tc => tc.UserId == userId && 
                   (tc.Status == ValidationStatus.AutoApproved || tc.Status == ValidationStatus.Validated))
            .OrderByDescending(tc => tc.CompletedAt)
            .Select(tc => tc.CompletedAt.Date)
            .Distinct()
            .Take(60)
            .ToListAsync();

        if (completions.Count == 0) return 0;

        var streak = 0;
        var expectedDate = DateTime.UtcNow.Date;

        if (completions.FirstOrDefault() != expectedDate)
        {
            expectedDate = expectedDate.AddDays(-1);
        }

        foreach (var date in completions)
        {
            if (date == expectedDate)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else if (date < expectedDate)
            {
                break;
            }
        }

        return streak;
    }
}
