namespace FitPlay.Domain.Models;

public class ClientProfile
{
    public int Id { get; set; }
    public int UserId { get; set; } 
    public DateTime? DateOfBirth { get; set; }
    public double? HeightCm { get; set; }
    public double? WeightKg { get; set; }
}