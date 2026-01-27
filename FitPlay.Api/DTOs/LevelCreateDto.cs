using System.ComponentModel.DataAnnotations;

namespace FitPlay.Api.DTOs;

public class LevelCreateDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public int ExperiencePoints { get; set; }
}
