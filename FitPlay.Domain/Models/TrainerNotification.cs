using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public class TrainerNotification
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(450)]
    public string TrainerId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(450)]
    public string SenderGymAdminId { get; set; } = string.Empty;
    
    public int GymLocationId { get; set; }
    
    [Required]
    [MaxLength(450)]
    public string SubjectUserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public GymLocation? GymLocation { get; set; }
}