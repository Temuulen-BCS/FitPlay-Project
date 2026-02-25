using System.ComponentModel.DataAnnotations;

namespace FitPlay.Api.DTOs;

public class TeacherCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// ASP.NET Identity user ID to link this domain record to an Identity account.
    /// </summary>
    [MaxLength(450)]
    public string? IdentityUserId { get; set; }
}
