using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public class GymLocation
{
    public int Id { get; set; }
    public int GymId { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; } = true;

    public Gym? Gym { get; set; }
    public List<Room> Rooms { get; set; } = new();
}
