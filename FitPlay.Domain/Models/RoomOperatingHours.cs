using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public class RoomOperatingHours
{
    public int Id { get; set; }
    
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    public TimeOnly OpenTime { get; set; }
    
    public TimeOnly CloseTime { get; set; }
    
    public bool IsClosed { get; set; }
    
    public Room? Room { get; set; }
}
