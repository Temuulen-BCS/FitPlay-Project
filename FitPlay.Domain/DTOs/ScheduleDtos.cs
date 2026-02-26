using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record ClassScheduleDto(
    int Id,
    int UserId,
    string Modality,
    DateTime ScheduledAt,
    string Status,
    string? Notes
);

public record CreateClassScheduleRequest(
    [Required] int UserId,
    [Required][MaxLength(20)] string Modality,
    DateTime ScheduledAt,
    string? Notes
);

public record UpdateClassScheduleRequest(
    [Required][MaxLength(20)] string Modality,
    DateTime ScheduledAt,
    string Status,
    string? Notes
);
