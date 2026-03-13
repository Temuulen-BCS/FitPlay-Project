using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface IPaymentService
{
    Task<ClassEnrollmentResponseDto> ConfirmEnrollmentPaymentAsync(int enrollmentId, string stripePaymentIntentId, decimal paidAmount);
    Task<PaymentSplitResponseDto> ProcessSessionSplitAsync(ProcessPaymentSplitRequest request);
    Task<TrainerEarningsSummaryDto> GetTrainerEarningsAsync(string trainerId, DateTime? from = null, DateTime? to = null);
}

