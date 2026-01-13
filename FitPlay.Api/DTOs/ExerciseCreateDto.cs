using System.ComponentModel.DataAnnotations;

namespace FitPlay.Api.DTOs;

public class ExerciseCreateDto
{
    [Required]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Difficulty { get; set; }

    [Range(0, 100000)]
    public int BasePoints { get; set; } = 100;

    [Range(1, 1000)]
    public int SuggestedDurationMin { get; set; } = 20;

    public bool IsActive { get; set; } = true;
}
