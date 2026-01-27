using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

/// <summary>
/// Service for managing user progress, XP, and levels.
/// </summary>
public class ProgressService
{
    private readonly FitPlayContext _db;

    public ProgressService(FitPlayContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get or create a user's level record.
    /// </summary>
    public async Task<UserLevel> GetOrCreateUserLevelAsync(int userId)
    {
        var userLevel = await _db.UserLevels.FirstOrDefaultAsync(ul => ul.UserId == userId);
        if (userLevel == null)
        {
            userLevel = new UserLevel { UserId = userId };
            _db.UserLevels.Add(userLevel);
            await _db.SaveChangesAsync();
        }
        return userLevel;
    }

    /// <summary>
    /// Get user's full progress summary.
    /// </summary>
    public async Task<UserProgressDto> GetUserProgressAsync(int userId)
    {
        var userLevel = await GetOrCreateUserLevelAsync(userId);
        var completionsCount = await _db.TrainingCompletions
            .CountAsync(tc => tc.UserId == userId && tc.Status == ValidationStatus.AutoApproved || tc.Status == ValidationStatus.Validated);
        
        var streak = await CalculateCurrentStreakAsync(userId);
        
        var currentLevelDef = LevelDefinition.DefaultLevels.First(l => l.Level == userLevel.CurrentLevel);
        var xpForCurrentLevel = currentLevelDef.MinXp;
        var xpForNextLevel = LevelDefinition.GetNextLevelXp(userLevel.CurrentLevel);
        var xpProgress = userLevel.TotalXp - xpForCurrentLevel;
        var xpNeeded = xpForNextLevel - xpForCurrentLevel;
        var progressPercent = xpNeeded > 0 ? Math.Min(100.0, (double)xpProgress / xpNeeded * 100) : 100.0;

        return new UserProgressDto(
            UserId: userId,
            CurrentLevel: userLevel.CurrentLevel,
            LevelLabel: LevelDefinition.GetLevelLabel(userLevel.CurrentLevel),
            TotalXp: userLevel.TotalXp,
            XpForCurrentLevel: xpForCurrentLevel,
            XpForNextLevel: xpForNextLevel,
            XpProgress: xpProgress,
            ProgressPercent: Math.Round(progressPercent, 1),
            TotalTrainingsCompleted: completionsCount,
            CurrentStreak: streak,
            LastUpdated: userLevel.LastUpdated
        );
    }

    /// <summary>
    /// Add XP to a user and recalculate level.
    /// </summary>
    public async Task<(int newTotalXp, int newLevel, bool leveledUp)> AddXpAsync(
        int userId, 
        int xpAmount, 
        XpTransactionType transactionType, 
        int? sourceId = null, 
        string? reason = null, 
        int? awardedByTrainerId = null)
    {
        var userLevel = await GetOrCreateUserLevelAsync(userId);
        var oldLevel = userLevel.CurrentLevel;
        var oldXp = userLevel.TotalXp;

        userLevel.TotalXp += xpAmount;
        if (userLevel.TotalXp < 0) userLevel.TotalXp = 0;
        
        userLevel.CurrentLevel = LevelDefinition.GetLevelFromXp(userLevel.TotalXp);
        userLevel.LastUpdated = DateTime.UtcNow;

        // Record transaction
        var transaction = new XpTransaction
        {
            UserId = userId,
            TransactionType = transactionType,
            SourceId = sourceId,
            XpDelta = xpAmount,
            XpBefore = oldXp,
            XpAfter = userLevel.TotalXp,
            Reason = reason,
            AwardedByTrainerId = awardedByTrainerId
        };
        _db.XpTransactions.Add(transaction);

        await _db.SaveChangesAsync();

        return (userLevel.TotalXp, userLevel.CurrentLevel, userLevel.CurrentLevel > oldLevel);
    }

    /// <summary>
    /// Reset a user's XP (trainer action).
    /// </summary>
    public async Task<(int newTotalXp, int newLevel)> ResetXpAsync(
        int userId, 
        int? newXpValue, 
        string reason, 
        int trainerId)
    {
        var userLevel = await GetOrCreateUserLevelAsync(userId);
        var oldXp = userLevel.TotalXp;
        var targetXp = newXpValue ?? 0;
        var delta = targetXp - oldXp;

        userLevel.TotalXp = targetXp;
        userLevel.CurrentLevel = LevelDefinition.GetLevelFromXp(targetXp);
        userLevel.LastUpdated = DateTime.UtcNow;

        var transaction = new XpTransaction
        {
            UserId = userId,
            TransactionType = XpTransactionType.Reset,
            XpDelta = delta,
            XpBefore = oldXp,
            XpAfter = targetXp,
            Reason = reason,
            AwardedByTrainerId = trainerId
        };
        _db.XpTransactions.Add(transaction);

        await _db.SaveChangesAsync();

        return (userLevel.TotalXp, userLevel.CurrentLevel);
    }

    /// <summary>
    /// Get XP transaction history for a user.
    /// </summary>
    public async Task<List<XpTransactionDto>> GetXpHistoryAsync(int userId, int limit = 50)
    {
        var transactions = await _db.XpTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Include(t => t.AwardedByTrainer)
            .ToListAsync();

        return transactions.Select(t => new XpTransactionDto(
            t.Id,
            t.TransactionType.ToString(),
            t.XpDelta,
            t.XpBefore,
            t.XpAfter,
            t.Reason,
            t.AwardedByTrainer?.Name,
            t.CreatedAt
        )).ToList();
    }

    /// <summary>
    /// Calculate the user's current streak (consecutive days with completions).
    /// </summary>
    private async Task<int> CalculateCurrentStreakAsync(int userId)
    {
        var completions = await _db.TrainingCompletions
            .Where(tc => tc.UserId == userId && 
                   (tc.Status == ValidationStatus.AutoApproved || tc.Status == ValidationStatus.Validated))
            .OrderByDescending(tc => tc.CompletedAt)
            .Select(tc => tc.CompletedAt.Date)
            .Distinct()
            .Take(60) // Look back max 60 days
            .ToListAsync();

        if (completions.Count == 0) return 0;

        var streak = 0;
        var expectedDate = DateTime.UtcNow.Date;

        // If no completion today, start from yesterday
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
