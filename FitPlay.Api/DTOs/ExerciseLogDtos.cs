namespace FitPlay.Api.DTOs;

public record ExerciseLogWithExerciseDto(
    int Id,
    int ClientId,
    int ExerciseId,
    string ExerciseTitle,
    string ExerciseCategory,
    DateTime PerformedAt,
    int DurationMin,
    int PointsAwarded,
    string? Notes
);

public record ExerciseLogSummaryDto(
    int TotalWorkouts,
    int TotalMinutes,
    int TotalPoints
);
