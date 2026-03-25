using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitPlay.Domain.Data;
using FitPlay.Api.Services;
using Stripe;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/classeschedules")]
public class ClassSchedulesController : ControllerBase
{
    private readonly ClassScheduleService _scheduleService;
    private readonly FitPlayContext _db;
    private readonly IPaymentService _paymentService;

    public ClassSchedulesController(ClassScheduleService scheduleService, FitPlayContext db, IPaymentService paymentService)
    {
        _scheduleService = scheduleService;
        _db = db;
        _paymentService = paymentService;
    }

    [HttpGet("public")]
    public async Task<ActionResult<List<ClassScheduleWithTrainerDto>>> GetPublicSchedules(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _scheduleService.GetPublicSchedulesAsync(from, to);
        return Ok(result);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<ClassScheduleWithTrainerDto>>> GetUserSchedule(
        int userId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _scheduleService.GetUserScheduleWithTrainerAsync(userId, from, to);
        return Ok(result);
    }

    [HttpGet("trainer/{trainerId:int}")]
    public async Task<ActionResult<List<ClassScheduleDto>>> GetTrainerSchedule(
        int trainerId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _scheduleService.GetTrainerScheduleAsync(trainerId, from, to);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClassScheduleDto>> GetById(int id)
    {
        var result = await _scheduleService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ClassScheduleDto>> Create([FromBody] CreateClassScheduleRequest request)
    {
        var result = await _scheduleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Books a FREE class directly (no price set). Returns 400 if the class has a price.
    /// </summary>
    [HttpPost("{id:int}/book")]
    public async Task<ActionResult<ClassScheduleDto>> BookClass(int id, [FromBody] BookClassRequest request)
    {
        var entity = await _scheduleService.GetEntityByIdAsync(id);
        if (entity == null) return NotFound("Class not found.");

        // If the class has a price, force payment flow
        var price = ClassScheduleService.ExtractPriceFromNotes(entity.Notes);
        if (price.HasValue && price.Value > 0)
            return BadRequest(new { message = "This class requires payment. Use the create-payment-intent endpoint." });

        var result = await _scheduleService.BookClassAsync(id, request.UserId);
        if (result == null) return NotFound("Class not found or already booked.");
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClassScheduleDto>> Update(int id, [FromBody] UpdateClassScheduleRequest request)
    {
        var result = await _scheduleService.UpdateAsync(id, request);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ClassScheduleDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var result = await _scheduleService.UpdateStatusAsync(id, request.Status);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Cancels a booking. If the booking had a completed payment, issues an 85% Stripe refund.
    /// </summary>
    [HttpPost("{id:int}/unbook")]
    public async Task<ActionResult<ClassScheduleDto>> Unbook(int id)
    {
        var entity = await _scheduleService.GetEntityByIdAsync(id);
        if (entity == null) return NotFound();

        // If a payment was completed, issue the 85% refund first
        if (entity.PaymentStatus == ClassSchedulePaymentStatus.Completed)
        {
            try
            {
                await _paymentService.RefundClassSchedulePaymentAsync(id);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Refund failed: {ex.Message}" });
            }

            var refunded = await _scheduleService.MarkRefundedAsync(id);
            return Ok(refunded);
        }

        // Free booking: just unbook
        var result = await _scheduleService.UnbookFreeAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _scheduleService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    // ── Stripe Payment Endpoints ──

    /// <summary>
    /// Creates a Stripe PaymentIntent for a paid class. Returns the client secret.
    /// </summary>
    [HttpPost("{id:int}/create-payment-intent")]
    public async Task<ActionResult<CreateClassPaymentIntentResponse>> CreatePaymentIntent(
        int id,
        [FromBody] CreateClassPaymentIntentRequest request)
    {
        try
        {
            var (schedule, price) = await _scheduleService.ValidateForPaymentAsync(id, request.UserId);

            // Reuse an existing pending PaymentIntent if possible
            if (!string.IsNullOrWhiteSpace(schedule.StripePaymentIntentId))
            {
                var existingService = new PaymentIntentService();
                var existingPi = await existingService.GetAsync(schedule.StripePaymentIntentId);
                if (existingPi.Status is "requires_payment_method" or "requires_confirmation" or "requires_action")
                {
                    return Ok(new CreateClassPaymentIntentResponse(existingPi.ClientSecret, price, "eur"));
                }
            }

            var amountInCents = Convert.ToInt64(decimal.Round(price * 100m, 0, MidpointRounding.AwayFromZero));

            var piService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "eur",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string>
                {
                    ["scheduleId"] = id.ToString(),
                    ["userId"] = request.UserId.ToString()
                }
            };

            var paymentIntent = await piService.CreateAsync(options);

            await _scheduleService.SetPaymentIntentAsync(id, paymentIntent.Id);

            return Ok(new CreateClassPaymentIntentResponse(paymentIntent.ClientSecret, price, "eur"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (StripeException ex)
        {
            return BadRequest(new { message = $"Payment error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Confirms payment, books the user onto the class, and processes revenue splits.
    /// </summary>
    [HttpPost("{id:int}/confirm-payment")]
    public async Task<ActionResult<ClassScheduleDto>> ConfirmPayment(
        int id,
        [FromBody] ConfirmClassPaymentRequest request)
    {
        try
        {
            var (schedule, price) = await _scheduleService.ValidateForPaymentAsync(id, request.UserId);

            // Resolve trainer's gym for revenue split
            string? trainerStripeAccountId = null;
            string? gymStripeAccountId = null;
            decimal gymCommissionRate = 0m;

            if (schedule.TrainerId.HasValue)
            {
                var gymLink = await _db.TrainerGymLinks
                    .Include(l => l.Gym)
                    .Where(l => l.TrainerId == schedule.Trainer!.IdentityUserId &&
                                l.Status == TrainerGymLinkStatus.Approved)
                    .FirstOrDefaultAsync();

                if (gymLink?.Gym != null)
                {
                    gymStripeAccountId = gymLink.Gym.StripeAccountId;
                    gymCommissionRate = gymLink.Gym.CommissionRate;
                    // Trainer Stripe account would be stored on the trainer profile if available
                    // For now we skip the transfer if not configured
                }
            }

            await _paymentService.ConfirmClassSchedulePaymentAsync(
                id,
                request.UserId,
                request.StripePaymentIntentId,
                price,
                trainerStripeAccountId,
                gymCommissionRate,
                gymStripeAccountId);

            var result = await _scheduleService.ConfirmPaidBookingAsync(id, request.UserId, request.StripePaymentIntentId, price);
            if (result == null) return NotFound("Class not found.");

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (StripeException ex)
        {
            return BadRequest(new { message = $"Payment error: {ex.Message}" });
        }
    }
}

public record UpdateStatusRequest(string Status);
