using System.ComponentModel.DataAnnotations;

namespace FitPlay.Api.DTOs;

public class TrainingCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int DurationMin { get; set; }

    [Range(0, 100000)]
    public int Points { get; set; }

    [MaxLength(200)]
    public string Athletes { get; set; } = string.Empty;
}
