namespace FitPlay.Api.DTOs;

public record CreateLogDto
    (int ClientId, int ExerciseId, int DurationMin, string? Notes);
