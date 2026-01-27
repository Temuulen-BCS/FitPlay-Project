using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

/// <summary>
/// Service for handling training completions.
/// </summary>
public class TrainingCompletionService
{
    private readonly FitPlayContext _db;
    private readonly ProgressService _progressService;
    private readonly AchievementService _achievementService;

    public TrainingCompletionService(
        FitPlayContext db, 
        ProgressService progressService,
        AchievementService achievementService)
    {
        _db = db;
        _progressService = progressService;
        _achievementService = achievementService;
    }

    /// <summary>
    /// Complete a training for a user.
    /// </summary>
    public async Task<CompleteTrainingResponse> CompleteTrainingAsync(int userId, CompleteTrainingRequest request)
    {
        var training = await _db.Trainings.FindAsync(request.TrainingId);
        if (training == null)
            throw new ArgumentException("Training not found");

        // Determine status based on training settings
        var status = training.RequiresValidation 
            ? ValidationStatus.Pending 
            : ValidationStatus.AutoApproved;

        var completion = new TrainingCompletion
        {
            TrainingId = request.TrainingId,
            UserId = userId,
            XpGranted = training.XpReward,
            Status = status,
            Notes = request.Notes
        };

        _db.TrainingCompletions.Add(completion);
        await _db.SaveChangesAsync();

        int newTotalXp = 0;
        int newLevel = 1;
        bool leveledUp = false;
        List<AchievementDto>? newAchievements = null;

        // Only award XP if auto-approved
        if (status == ValidationStatus.AutoApproved)
        {
            (newTotalXp, newLevel, leveledUp) = await _progressService.AddXpAsync(
                userId, 
                training.XpReward, 
                XpTransactionType.TrainingCompletion,
                sourceId: completion.Id,
                reason: $"Completed training: {training.Name}"
            );

            // Check for new achievements
            newAchievements = await _achievementService.CheckAndAwardAchievementsAsync(userId, newLevel, leveledUp);
        }
        else
        {
            var progress = await _progressService.GetUserProgressAsync(userId);
            newTotalXp = progress.TotalXp;
            newLevel = progress.CurrentLevel;
        }

        return new CompleteTrainingResponse(
            CompletionId: completion.Id,
            XpAwarded: status == ValidationStatus.AutoApproved ? training.XpReward : 0,
            Status: status.ToString(),
            NewTotalXp: newTotalXp,
            NewLevel: newLevel,
            LeveledUp: leveledUp,
            NewAchievements: newAchievements
        );
    }

    /// <summary>
    /// Validate a pending completion (trainer action).
    /// </summary>
    public async Task<CompleteTrainingResponse> ValidateCompletionAsync(int trainerId, ValidateCompletionRequest request)
    {
        var completion = await _db.TrainingCompletions
            .Include(c => c.Training)
            .FirstOrDefaultAsync(c => c.Id == request.CompletionId);

        if (completion == null)
            throw new ArgumentException("Completion not found");

        if (completion.Status != ValidationStatus.Pending)
            throw new InvalidOperationException("Completion is not pending validation");

        completion.ValidatedByTrainerId = trainerId;
        completion.ValidatedAt = DateTime.UtcNow;

        int newTotalXp = 0;
        int newLevel = 1;
        bool leveledUp = false;
        List<AchievementDto>? newAchievements = null;

        if (request.Approved)
        {
            completion.Status = ValidationStatus.Validated;
            
            // Apply XP adjustment if any
            var xpToAward = request.XpAdjustment ?? completion.XpGranted;
            completion.XpGranted = xpToAward;

            if (!string.IsNullOrEmpty(request.Notes))
                completion.Notes = request.Notes;

            (newTotalXp, newLevel, leveledUp) = await _progressService.AddXpAsync(
                completion.UserId,
                xpToAward,
                XpTransactionType.TrainingCompletion,
                sourceId: completion.Id,
                reason: $"Validated training: {completion.Training?.Name}",
                awardedByTrainerId: trainerId
            );

            newAchievements = await _achievementService.CheckAndAwardAchievementsAsync(
                completion.UserId, newLevel, leveledUp);
        }
        else
        {
            completion.Status = ValidationStatus.Rejected;
            completion.XpGranted = 0;
            completion.Notes = request.Notes ?? "Rejected by trainer";

            var progress = await _progressService.GetUserProgressAsync(completion.UserId);
            newTotalXp = progress.TotalXp;
            newLevel = progress.CurrentLevel;
        }

        await _db.SaveChangesAsync();

        return new CompleteTrainingResponse(
            CompletionId: completion.Id,
            XpAwarded: completion.XpGranted,
            Status: completion.Status.ToString(),
            NewTotalXp: newTotalXp,
            NewLevel: newLevel,
            LeveledUp: leveledUp,
            NewAchievements: newAchievements
        );
    }

    /// <summary>
    /// Get completions for a user.
    /// </summary>
    public async Task<List<TrainingCompletionDto>> GetUserCompletionsAsync(int userId, int limit = 50)
    {
        var completions = await _db.TrainingCompletions
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CompletedAt)
            .Take(limit)
            .Include(c => c.Training)
            .ToListAsync();

        return completions.Select(c => new TrainingCompletionDto(
            c.Id,
            c.TrainingId,
            c.Training?.Name ?? "Unknown",
            c.UserId,
            c.CompletedAt,
            c.XpGranted,
            c.Status.ToString(),
            c.ValidatedByTrainerId,
            c.ValidatedAt,
            c.Notes
        )).ToList();
    }

    /// <summary>
    /// Get pending completions for a trainer to validate.
    /// </summary>
    public async Task<List<TrainingCompletionDto>> GetPendingValidationsAsync(int trainerId)
    {
        var completions = await _db.TrainingCompletions
            .Where(c => c.Status == ValidationStatus.Pending && c.Training!.TrainerId == trainerId)
            .OrderByDescending(c => c.CompletedAt)
            .Include(c => c.Training)
            .Include(c => c.User)
            .ToListAsync();

        return completions.Select(c => new TrainingCompletionDto(
            c.Id,
            c.TrainingId,
            c.Training?.Name ?? "Unknown",
            c.UserId,
            c.CompletedAt,
            c.XpGranted,
            c.Status.ToString(),
            c.ValidatedByTrainerId,
            c.ValidatedAt,
            c.Notes
        )).ToList();
    }
}
