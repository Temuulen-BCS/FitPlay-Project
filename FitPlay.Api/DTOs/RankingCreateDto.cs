using System.ComponentModel.DataAnnotations;

namespace FitPlay.Api.DTOs;

public class RankingCreateDto
{
    [Required]
    [MaxLength(100)]
    public string User { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public int Points { get; set; }
}
