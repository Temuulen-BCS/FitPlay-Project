using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

/// <summary>
/// Request to complete a training.
/// </summary>
public record CompleteTrainingRequest(
    [Required] int TrainingId,
    string? Notes
);

/// <summary>
/// Response after completing a training.
/// </summary>
public record CompleteTrainingResponse(
    int CompletionId,
    int XpAwarded,
    string Status,
    int NewTotalXp,
    int NewLevel,
    bool LeveledUp,
    List<AchievementDto>? NewAchievements
);

/// <summary>
/// Request to validate a training completion (trainer action).
/// </summary>
public record ValidateCompletionRequest(
    [Required] int CompletionId,
    [Required] bool Approved,
    int? XpAdjustment,
    string? Notes
);

/// <summary>
/// Request to award bonus XP (trainer action).
/// </summary>
public record AwardBonusXpRequest(
    [Required] int UserId,
    [Required][Range(1, 10000)] int XpAmount,
    [Required] string Reason
);

/// <summary>
/// Request to reset a user's XP (trainer action).
/// </summary>
public record ResetXpRequest(
    [Required] int UserId,
    [Required] string Reason,
    int? NewXpValue
);

/// <summary>
/// Request to create a new training (trainer action).
/// </summary>
public record CreateTrainingRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(2000)] string? Description,
    [Range(1, 480)] int DurationMin,
    [Required][Range(1, 10000)] int XpReward,
    [Range(1, 5)] int Difficulty,
    bool RequiresValidation,
    List<CreateTrainingExerciseRequest> Exercises
);

/// <summary>
/// Exercise within a create training request.
/// </summary>
public record CreateTrainingExerciseRequest(
    [Required] int ExerciseId,
    int SortOrder,
    [Range(1, 20)] int Sets,
    [Range(1, 100)] int Reps,
    [Range(0, 300)] int RestSeconds,
    string? Notes
);

/// <summary>
/// Request to update a training (trainer action).
/// </summary>
public record UpdateTrainingRequest(
    [MaxLength(200)] string? Name,
    [MaxLength(2000)] string? Description,
    [Range(1, 480)] int? DurationMin,
    [Range(1, 10000)] int? XpReward,
    [Range(1, 5)] int? Difficulty,
    bool? RequiresValidation,
    bool? IsActive
);
