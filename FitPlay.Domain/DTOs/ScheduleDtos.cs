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
    decimal? PaidAmount = null,
    double? DurationMinutes = null
);

public record CreateClassScheduleRequest(
    int? UserId,
    int? TrainerId,
    [Required][MaxLength(20)] string Modality,
    DateTime ScheduledAt,
    string? Notes,
    int? RoomBookingId = null
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
    decimal? PaidAmount = null,
    string? RoomBookingStatus = null,
    int QueueCount = 0,
    double? DurationMinutes = null
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

// Queue DTOs
public record JoinQueueResponse(
    int QueueEntryId,
    decimal QueueCost,
    bool HasMembership,
    string? ClientSecret,
    int MonthlySkipCount = 0
);

public record QueueCountResponse(
    int ClassScheduleId,
    int Count
);

public record ConfirmQueuePaymentRequest(
    [Required] string StripePaymentIntentId
);

public record UserQueueEntryDto(
    int ClassScheduleId,
    bool IsNotified,
    decimal QueueCost,
    bool IsSkipped = false
);
