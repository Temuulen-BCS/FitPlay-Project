using FitPlay.Api;
using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FitPlay.Api.Services;

public class PaymentService : IPaymentService
{
    private const decimal PlatformRate = 0.10m;
    private const decimal ClassScheduleRefundRate = 0.85m;

    private readonly FitPlayContext _db;
    private readonly StripeOptions _stripeOptions;

    public PaymentService(FitPlayContext db, IOptions<StripeOptions> stripeOptions)
    {
        _db = db;
        _stripeOptions = stripeOptions.Value;

        if (!string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
        {
            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
        }
    }

    public async Task<ClassEnrollmentResponseDto> ConfirmEnrollmentPaymentAsync(int enrollmentId, string stripePaymentIntentId, decimal paidAmount)
    {
        var enrollment = await _db.ClassEnrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment is null)
            throw new ArgumentException("Enrollment not found.");

        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
            throw new ArgumentException("StripePaymentIntentId is required.");

        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.GetAsync(stripePaymentIntentId.Trim());

        if (!string.Equals(paymentIntent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PaymentIntent is not confirmed by Stripe.");

        var expectedAmountInCents = Convert.ToInt64(decimal.Round(paidAmount * 100m, 0, MidpointRounding.AwayFromZero));
        if (paymentIntent.AmountReceived < expectedAmountInCents)
            throw new InvalidOperationException("Stripe amount received is lower than expected.");

        enrollment.Status = ClassEnrollmentStatus.Confirmed;
        enrollment.PaidAmount = paidAmount;
        enrollment.StripePaymentIntentId = paymentIntent.Id;

        await _db.SaveChangesAsync();

        return ToEnrollmentDto(enrollment);
    }

    public async Task<PaymentSplitResponseDto> ProcessSessionSplitAsync(ProcessPaymentSplitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrainerStripeAccountId))
            throw new ArgumentException("TrainerStripeAccountId is required.");

        var enrollment = await _db.ClassEnrollments
            .Include(e => e.ClassSession)
            .ThenInclude(s => s!.RoomBooking)
            .ThenInclude(b => b!.Room)
            .ThenInclude(r => r!.GymLocation)
            .ThenInclude(gl => gl!.Gym)
            .Include(e => e.PaymentSplit)
            .FirstOrDefaultAsync(e => e.Id == request.ClassEnrollmentId);

        if (enrollment is null)
            throw new ArgumentException("Enrollment not found.");

        if (enrollment.ClassSession is null)
            throw new InvalidOperationException("Enrollment has no class session.");

        if (enrollment.ClassSession.Status != ClassSessionStatus.Completed)
            throw new InvalidOperationException("Split can only be processed after session is completed.");

        if (enrollment.Status != ClassEnrollmentStatus.Confirmed && enrollment.Status != ClassEnrollmentStatus.Completed)
            throw new InvalidOperationException("Enrollment payment is not confirmed.");

        if (enrollment.PaymentSplit is not null)
            return ToSplitDto(enrollment.PaymentSplit);

        var gym = enrollment.ClassSession.RoomBooking?.Room?.GymLocation?.Gym;
        if (gym is null)
            throw new InvalidOperationException("Gym not found for this enrollment.");

        if (string.IsNullOrWhiteSpace(gym.StripeAccountId))
            throw new InvalidOperationException("Gym Stripe account is not configured.");

        var total = enrollment.PaidAmount;
        if (total <= 0)
            throw new InvalidOperationException("PaidAmount must be greater than zero.");

        var gymRate = gym.CommissionRate;
        var trainerRate = 1m - gymRate - PlatformRate;
        if (trainerRate < 0)
            throw new InvalidOperationException("Invalid split rates configuration.");

        var gymAmount = decimal.Round(total * gymRate, 2, MidpointRounding.AwayFromZero);
        var platformAmount = decimal.Round(total * PlatformRate, 2, MidpointRounding.AwayFromZero);
        var trainerAmount = total - gymAmount - platformAmount;

        var transferService = new TransferService();

        var gymTransfer = await transferService.CreateAsync(new TransferCreateOptions
        {
            Amount = ToStripeAmount(gymAmount),
            Currency = "eur",
            Destination = gym.StripeAccountId,
            Metadata = new Dictionary<string, string>
            {
                ["enrollmentId"] = enrollment.Id.ToString(),
                ["type"] = "gym_share"
            }
        });

        var trainerTransfer = await transferService.CreateAsync(new TransferCreateOptions
        {
            Amount = ToStripeAmount(trainerAmount),
            Currency = "eur",
            Destination = request.TrainerStripeAccountId.Trim(),
            Metadata = new Dictionary<string, string>
            {
                ["enrollmentId"] = enrollment.Id.ToString(),
                ["type"] = "trainer_share"
            }
        });

        var split = new PaymentSplit
        {
            ClassEnrollmentId = enrollment.Id,
            GymAmount = gymAmount,
            TrainerAmount = trainerAmount,
            PlatformAmount = platformAmount,
            StripeTransferId = $"{gymTransfer.Id};{trainerTransfer.Id}",
            ProcessedAt = DateTime.UtcNow
        };

        _db.PaymentSplits.Add(split);
        await _db.SaveChangesAsync();

        return ToSplitDto(split);
    }

    public async Task<TrainerEarningsSummaryDto> GetTrainerEarningsAsync(string trainerId, DateTime? from = null, DateTime? to = null)
    {
        var normalizedTrainerId = trainerId.Trim();

        var query = _db.PaymentSplits
            .AsNoTracking()
            .Include(ps => ps.ClassEnrollment)
            .ThenInclude(e => e!.ClassSession)
            .Where(ps => ps.ClassEnrollment != null && ps.ClassEnrollment.ClassSession != null)
            .Where(ps => ps.ClassEnrollment!.ClassSession!.TrainerId == normalizedTrainerId)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(ps => ps.ProcessedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(ps => ps.ProcessedAt <= to.Value);
        }

        var splits = await query
            .OrderByDescending(ps => ps.ProcessedAt)
            .ToListAsync();

        var items = splits.Select(ps => new TrainerEarningsItemDto(
            ps.ClassEnrollment!.ClassSessionId,
            ps.ClassEnrollment.ClassSession!.Title,
            ps.ClassEnrollment.ClassSession.StartTime,
            ps.TrainerAmount,
            ps.ProcessedAt
        )).ToList();

        return new TrainerEarningsSummaryDto(
            normalizedTrainerId,
            items.Sum(i => i.Amount),
            items
        );
    }

    // ── Class Schedule (Book-Class) Payment ──

    /// <summary>
    /// Verifies the Stripe PaymentIntent succeeded, then optionally creates revenue splits.
    /// Returns the Stripe PaymentIntent ID on success.
    /// </summary>
    public async Task<string> ConfirmClassSchedulePaymentAsync(
        int scheduleId,
        int userId,
        string stripePaymentIntentId,
        decimal paidAmount,
        string? trainerStripeAccountId,
        decimal gymCommissionRate,
        string? gymStripeAccountId)
    {
        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
            throw new ArgumentException("StripePaymentIntentId is required.");

        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.GetAsync(stripePaymentIntentId.Trim());

        if (!string.Equals(paymentIntent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PaymentIntent has not been confirmed by Stripe.");

        var expectedCents = Convert.ToInt64(decimal.Round(paidAmount * 100m, 0, MidpointRounding.AwayFromZero));
        if (paymentIntent.AmountReceived < expectedCents)
            throw new InvalidOperationException("Stripe amount received is lower than expected.");

        // Process revenue splits if trainer/gym Stripe accounts are available
        if (!string.IsNullOrWhiteSpace(trainerStripeAccountId) &&
            !string.IsNullOrWhiteSpace(gymStripeAccountId) &&
            gymCommissionRate > 0)
        {
            var trainerRate = 1m - gymCommissionRate - PlatformRate;
            if (trainerRate > 0)
            {
                var gymAmount = decimal.Round(paidAmount * gymCommissionRate, 2, MidpointRounding.AwayFromZero);
                var platformAmount = decimal.Round(paidAmount * PlatformRate, 2, MidpointRounding.AwayFromZero);
                var trainerAmount = paidAmount - gymAmount - platformAmount;

                var transferService = new TransferService();

                await transferService.CreateAsync(new TransferCreateOptions
                {
                    Amount = ToStripeAmount(gymAmount),
                    Currency = "eur",
                    Destination = gymStripeAccountId,
                    Metadata = new Dictionary<string, string>
                    {
                        ["scheduleId"] = scheduleId.ToString(),
                        ["userId"] = userId.ToString(),
                        ["type"] = "gym_share"
                    }
                });

                await transferService.CreateAsync(new TransferCreateOptions
                {
                    Amount = ToStripeAmount(trainerAmount),
                    Currency = "eur",
                    Destination = trainerStripeAccountId,
                    Metadata = new Dictionary<string, string>
                    {
                        ["scheduleId"] = scheduleId.ToString(),
                        ["userId"] = userId.ToString(),
                        ["type"] = "trainer_share"
                    }
                });
            }
        }

        return paymentIntent.Id;
    }

    /// <summary>
    /// Issues an 85% refund for a cancelled paid class schedule booking.
    /// </summary>
    public async Task<ClassPaymentRefundResponse> RefundClassSchedulePaymentAsync(int scheduleId)
    {
        var schedule = await _db.ClassSchedules.FindAsync(scheduleId)
            ?? throw new ArgumentException("Class schedule not found.");

        if (schedule.PaymentStatus != ClassSchedulePaymentStatus.Completed)
            throw new InvalidOperationException("This booking has no completed payment to refund.");

        if (string.IsNullOrWhiteSpace(schedule.StripePaymentIntentId))
            throw new InvalidOperationException("No Stripe PaymentIntent found for this booking.");

        if (!schedule.PaidAmount.HasValue || schedule.PaidAmount.Value <= 0)
            throw new InvalidOperationException("No valid paid amount found for refund.");

        var refundAmount = decimal.Round(schedule.PaidAmount.Value * ClassScheduleRefundRate, 2, MidpointRounding.AwayFromZero);

        var refundService = new RefundService();
        await refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = schedule.StripePaymentIntentId,
            Amount = ToStripeAmount(refundAmount),
            Metadata = new Dictionary<string, string>
            {
                ["scheduleId"] = scheduleId.ToString(),
                ["reason"] = "user_cancellation",
                ["refundRate"] = ClassScheduleRefundRate.ToString("F2")
            }
        });

        return new ClassPaymentRefundResponse(scheduleId, refundAmount, "Refunded");
    }

    private static long ToStripeAmount(decimal amount)
    {
        return Convert.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }

    private static PaymentSplitResponseDto ToSplitDto(PaymentSplit split) => new(
        split.Id,
        split.ClassEnrollmentId,
        split.GymAmount,
        split.TrainerAmount,
        split.PlatformAmount,
        split.StripeTransferId,
        split.ProcessedAt
    );

    private static ClassEnrollmentResponseDto ToEnrollmentDto(ClassEnrollment enrollment) => new(
        enrollment.Id,
        enrollment.ClassSessionId,
        enrollment.UserId,
        enrollment.Status.ToString(),
        enrollment.PaidAmount,
        enrollment.StripePaymentIntentId,
        enrollment.EnrolledAt
    );
}
