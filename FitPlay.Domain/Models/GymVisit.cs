namespace FitPlay.Domain.Models;

public class GymVisit
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int GymLocationId { get; set; }
    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOutTime { get; set; }
    public double CheckInLatitude { get; set; }
    public double CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }

    // Navigation properties
    public GymLocation? GymLocation { get; set; }
}