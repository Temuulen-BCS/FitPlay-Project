using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public class Gym
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(18)]
    public string CNPJ { get; set; } = string.Empty;

    public decimal CommissionRate { get; set; }
    public decimal CancelFeeRate { get; set; }

    [MaxLength(255)]
    public string? StripeAccountId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<GymLocation> GymLocations { get; set; } = new();
}
