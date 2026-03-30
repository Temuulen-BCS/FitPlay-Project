using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record GymResponseDto(
    int Id,
    string Name,
    string CNPJ,
    decimal CommissionRate,
    decimal CancelFeeRate,
    string? StripeAccountId,
    bool IsActive,
    string? OwnerUserId
);

public record CreateGymRequest(
    [Required][MaxLength(120)] string Name,
    [Required][MaxLength(18)] string CNPJ,
    decimal CommissionRate,
    decimal CancelFeeRate,
    [MaxLength(255)] string? StripeAccountId,
    bool IsActive = true
);

public record UpdateGymRequest(
    [Required][MaxLength(120)] string Name,
    [Required][MaxLength(18)] string CNPJ,
    decimal CommissionRate,
    decimal CancelFeeRate,
    [MaxLength(255)] string? StripeAccountId,
    bool IsActive
);

public record GymLocationResponseDto(
    int Id,
    int GymId,
    string Name,
    string Address,
    string City,
    string State,
    string ZipCode,
    double? Latitude,
    double? Longitude,
    bool IsActive
);

public record CreateGymLocationRequest(
    int GymId,
    [Required][MaxLength(120)] string Name,
    [Required][MaxLength(200)] string Address,
    [Required][MaxLength(100)] string City,
    [Required][MaxLength(100)] string State,
    [Required][MaxLength(20)] string ZipCode,
    double? Latitude,
    double? Longitude,
    bool IsActive = true
);

public record UpdateGymLocationRequest(
    [Required][MaxLength(120)] string Name,
    [Required][MaxLength(200)] string Address,
    [Required][MaxLength(100)] string City,
    [Required][MaxLength(100)] string State,
    [Required][MaxLength(20)] string ZipCode,
    double? Latitude,
    double? Longitude,
    bool IsActive
);

public record TrainerGymLinkResponseDto(
    int Id,
    string TrainerId,
    string TrainerName,
    string TrainerEmail,
    int GymId,
    string Status,
    DateTime CreatedAt
);

public record CreateTrainerGymLinkRequest(
    [Required][MaxLength(450)] string TrainerId,
    int GymId
);

public record UpdateTrainerGymLinkStatusRequest(
    [Required] string Status
);
