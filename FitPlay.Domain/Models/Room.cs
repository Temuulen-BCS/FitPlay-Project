using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public class Room
{
    public int Id { get; set; }
    public int GymLocationId { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Capacity { get; set; }
    public decimal PricePerHour { get; set; }
    public bool IsActive { get; set; } = true;

    public GymLocation? GymLocation { get; set; }
    public List<RoomBooking> Bookings { get; set; } = new();
}
