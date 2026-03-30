using System.Text;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/booking-webhook")]
public class RoomBookingWebhookController : ControllerBase
{
    private readonly FitPlayContext _db;
    private readonly StripeOptions _stripeOptions;
    private readonly IClockService _clock;

    public RoomBookingWebhookController(FitPlayContext db, IOptions<StripeOptions> stripeOptions, IClockService clock)
    {
        _db = db;
        _stripeOptions = stripeOptions.Value;
        _clock = clock;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
            return BadRequest(new { message = "Stripe WebhookSecret not configured." });

        var json = await new StreamReader(HttpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeOptions.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        if (stripeEvent.Type == "payment_intent.succeeded")
        {
            await HandlePaymentIntentSucceeded(stripeEvent);
        }

        return Ok();
    }

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent is null) return;

        // Check if this PaymentIntent has a bookingId in metadata
        if (!paymentIntent.Metadata.TryGetValue("bookingId", out var bookingIdStr))
            return;

        if (!int.TryParse(bookingIdStr, out var bookingId))
            return;

        var booking = await _db.RoomBookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return;

        // Idempotent: skip if already confirmed
        if (booking.Status == RoomBookingStatus.Confirmed)
            return;

        if (booking.Status != RoomBookingStatus.Pending)
            return;

        var expectedAmount = Convert.ToInt64(decimal.Round(booking.TotalCost * 100m, 0, MidpointRounding.AwayFromZero));
        if (paymentIntent.AmountReceived < expectedAmount)
            return;

        booking.Status = RoomBookingStatus.Confirmed;
        booking.StripePaymentIntentId = paymentIntent.Id;
        booking.PaidAmount = booking.TotalCost;
        booking.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync();
    }
}
