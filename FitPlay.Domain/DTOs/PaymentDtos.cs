namespace FitPlay.Domain.DTOs;

public record PaymentSplitResponseDto(
    int Id,
    int ClassEnrollmentId,
    decimal GymAmount,
    decimal TrainerAmount,
    decimal PlatformAmount,
    string? StripeTransferId,
    DateTime ProcessedAt
);

public record ProcessPaymentSplitRequest(
    int ClassEnrollmentId,
    string TrainerStripeAccountId
);

public record TrainerEarningsItemDto(
    int ClassSessionId,
    string SessionTitle,
    DateTime SessionDate,
    decimal Amount,
    DateTime ProcessedAt
);

public record TrainerEarningsSummaryDto(
    string TrainerId,
    decimal TotalAmount,
    List<TrainerEarningsItemDto> Items
);

