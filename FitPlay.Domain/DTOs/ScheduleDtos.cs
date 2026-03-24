using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record ClassScheduleDto(
    int Id,
    int? UserId,
    int? TrainerId,
    string Modality,
    DateTime ScheduledAt,
    string Status,
    string? Notes,
    string PaymentStatus = "None",
    decimal? PaidAmount = null
);

public record CreateClassScheduleRequest(
    int? UserId,
    int? TrainerId,
    [Required][MaxLength(20)] string Modality,
    DateTime ScheduledAt,
    string? Notes
);

public record UpdateClassScheduleRequest(
    [Required][MaxLength(20)] string Modality,
    DateTime ScheduledAt,
    string Status,
    string? Notes
);

public record BookClassRequest(
    [Required] int UserId
);

public record ClassScheduleWithTrainerDto(
    int Id,
    int? TrainerId,
    string TrainerName,
    string Modality,
    DateTime ScheduledAt,
    string Status,
    string? Notes,
    string PaymentStatus = "None",
    decimal? PaidAmount = null
);

// Payment DTOs
public record CreateClassPaymentIntentRequest(
    [Required] int UserId
);

public record CreateClassPaymentIntentResponse(
    string ClientSecret,
    decimal Amount,
    string Currency
);

public record ConfirmClassPaymentRequest(
    [Required] int UserId,
    [Required] string StripePaymentIntentId
);

public record ClassPaymentRefundResponse(
    int ScheduleId,
    decimal RefundAmount,
    string Status
);
