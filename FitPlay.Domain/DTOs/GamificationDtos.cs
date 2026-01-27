namespace FitPlay.Domain.DTOs;

/// <summary>
/// User's current progress in the gamification system.
/// </summary>
public record UserProgressDto(
    int UserId,
    int CurrentLevel,
    string LevelLabel,
    int TotalXp,
    int XpForCurrentLevel,
    int XpForNextLevel,
    int XpProgress,
    double ProgressPercent,
    int TotalTrainingsCompleted,
    int CurrentStreak,
    DateTime LastUpdated
);

/// <summary>
/// Summary of a training completion event.
/// </summary>
public record TrainingCompletionDto(
    int Id,
    int TrainingId,
    string TrainingName,
    int UserId,
    DateTime CompletedAt,
    int XpGranted,
    string Status,
    int? ValidatedByTrainerId,
    DateTime? ValidatedAt,
    string? Notes
);

/// <summary>
/// XP transaction history item.
/// </summary>
public record XpTransactionDto(
    int Id,
    string TransactionType,
    int XpDelta,
    int XpBefore,
    int XpAfter,
    string? Reason,
    string? AwardedByTrainerName,
    DateTime CreatedAt
);

/// <summary>
/// Achievement earned by a user.
/// </summary>
public record AchievementDto(
    int Id,
    string AchievementType,
    string Name,
    string Description,
    string? IconUrl,
    DateTime AwardedAt
);

/// <summary>
/// Training with exercises for display.
/// </summary>
public record TrainingDto(
    int Id,
    string Name,
    string Description,
    int DurationMin,
    int XpReward,
    int Difficulty,
    int TrainerId,
    string TrainerName,
    bool RequiresValidation,
    bool IsActive,
    List<TrainingExerciseDto> Exercises
);

/// <summary>
/// Exercise within a training.
/// </summary>
public record TrainingExerciseDto(
    int Id,
    int ExerciseId,
    string ExerciseTitle,
    string Category,
    int SortOrder,
    int Sets,
    int Reps,
    int RestSeconds,
    string? Notes
);

/// <summary>
/// Training list item (summary).
/// </summary>
public record TrainingSummaryDto(
    int Id,
    string Name,
    string Description,
    int DurationMin,
    int XpReward,
    int Difficulty,
    string TrainerName,
    int ExerciseCount,
    bool IsCompleted
);
