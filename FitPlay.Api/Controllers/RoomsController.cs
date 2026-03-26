using System.Security.Claims;
using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace FitPlay.Api.Controllers;

[ApiController]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ClassQueueService _queueService;
    private readonly FitPlayContext _db;

    public RoomsController(IRoomService roomService, ClassQueueService queueService, FitPlayContext db)
    {
        _roomService = roomService;
        _queueService = queueService;
        _db = db;
    }

    [HttpGet("/api/locations/{id:int}/rooms")]
    public async Task<ActionResult<List<RoomResponseDto>>> GetRoomsByLocation(int id, [FromQuery] bool? isActive = null)
    {
        var rooms = await _roomService.GetRoomsByLocationAsync(id, isActive);
        return Ok(rooms);
    }

    [HttpPost("/api/locations/{locationId:int}/rooms")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<RoomResponseDto>> CreateRoom(int locationId, [FromBody] CreateRoomRequest request)
    {
        var gymId = await _db.GymLocations.AsNoTracking()
            .Where(gl => gl.Id == locationId)
            .Select(gl => (int?)gl.GymId)
            .FirstOrDefaultAsync();

        if (!gymId.HasValue)
            return NotFound();

        var authResult = await EnsureCanManageGymAsync(gymId.Value);
        if (authResult is not null)
            return authResult;

        var req = request with { GymLocationId = locationId };

        try
        {
            var room = await _roomService.CreateRoomAsync(req);
            return Created($"/api/rooms/{room.Id}", room);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("/api/rooms/{id:int}")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<RoomResponseDto>> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        var gymId = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == id)
            .Join(_db.GymLocations.AsNoTracking(), r => r.GymLocationId, gl => gl.Id, (_, gl) => (int?)gl.GymId)
            .FirstOrDefaultAsync();

        if (!gymId.HasValue)
            return NotFound();

        var authResult = await EnsureCanManageGymAsync(gymId.Value);
        if (authResult is not null)
            return authResult;

        try
        {
            var room = await _roomService.UpdateRoomAsync(id, request);
            if (room is null) return NotFound();
            return Ok(room);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("/api/rooms/{id:int}")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var gymId = await _db.Rooms.AsNoTracking()
            .Where(r => r.Id == id)
            .Join(_db.GymLocations.AsNoTracking(), r => r.GymLocationId, gl => gl.Id, (_, gl) => (int?)gl.GymId)
            .FirstOrDefaultAsync();

        if (!gymId.HasValue)
            return NotFound();

        var authResult = await EnsureCanManageGymAsync(gymId.Value);
        if (authResult is not null)
            return authResult;

        var deleted = await _roomService.DeleteRoomAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("/api/rooms/{id:int}/availability")]
    public async Task<ActionResult<RoomAvailabilityResponseDto>> GetAvailability(int id, [FromQuery] DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var availability = await _roomService.GetRoomAvailabilityAsync(id, targetDate);
        return Ok(availability);
    }

    [HttpGet("/api/rooms/{id:int}/bookings")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<List<RoomBookingResponseDto>>> GetRoomBookings(
        int id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var bookings = await _roomService.GetRoomBookingsAsync(id, from, to);
        return Ok(bookings);
    }

    [HttpPost("/api/rooms/{id:int}/bookings")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<RoomBookingResponseDto>> CreateBooking(int id, [FromBody] CreateRoomBookingRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var booking = await _roomService.CreateBookingAsync(id, actorId, request);
            return Created($"/api/bookings/{booking.Id}", booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("/api/bookings/{id:int}")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var deleted = await _roomService.CancelBookingAsync(id, actorId, User.IsInRole("Admin"));
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("/api/bookings/mine")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<List<RoomBookingResponseDto>>> GetMyBookings(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var bookings = await _roomService.GetTrainerBookingsAsync(actorId, from, to);
        return Ok(bookings);
    }

    [HttpPost("/api/bookings/{id:int}/confirm")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<RoomBookingResponseDto>> ConfirmBooking(int id)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var result = await _roomService.ConfirmBookingAsync(id, actorId, User.IsInRole("Admin"));
            if (result is null) return NotFound();
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("/api/bookings/{id:int}/cancel-preview")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult> GetCancellationPreview(int id)
    {
        var booking = await _db.RoomBookings.AsNoTracking()
            .Include(b => b.Room)
                .ThenInclude(r => r!.GymLocation)
                    .ThenInclude(gl => gl!.Gym)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null) return NotFound();

        var gym = booking.Room?.GymLocation?.Gym;
        var cancelFeeRate = gym?.CancelFeeRate ?? 0m;
        var feeAmount = decimal.Round(booking.TotalCost * cancelFeeRate, 2, MidpointRounding.AwayFromZero);

        return Ok(new { CancelFeeRate = cancelFeeRate, FeeAmount = feeAmount });
    }

    // ── Stripe Payment for Room Bookings ──

    public record CreateBookingPaymentIntentResponse(string ClientSecret);
    public record ConfirmBookingPaymentRequest(string StripePaymentIntentId);

    [HttpPost("/api/bookings/{id:int}/create-payment-intent")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<CreateBookingPaymentIntentResponse>> CreateBookingPaymentIntent(int id)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var booking = await _db.RoomBookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound();

        if (!User.IsInRole("Admin") && booking.TrainerId != actorId.Trim())
            return Forbid();

        if (booking.Status != RoomBookingStatus.Pending)
            return BadRequest(new { message = "Only pending bookings can be paid." });

        // If a payment intent was already created, retrieve it
        if (!string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            var existingService = new PaymentIntentService();
            var existingPi = await existingService.GetAsync(booking.StripePaymentIntentId);

            if (existingPi.Status is "requires_payment_method" or "requires_confirmation" or "requires_action")
            {
                return Ok(new CreateBookingPaymentIntentResponse(existingPi.ClientSecret));
            }
        }

        var paymentIntentService = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = Convert.ToInt64(decimal.Round(booking.TotalCost * 100m, 0, MidpointRounding.AwayFromZero)),
            Currency = "eur",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                ["bookingId"] = booking.Id.ToString(),
                ["trainerId"] = booking.TrainerId
            }
        };

        var paymentIntent = await paymentIntentService.CreateAsync(options);

        // Store the PI id on the booking so we can reuse/verify later
        booking.StripePaymentIntentId = paymentIntent.Id;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new CreateBookingPaymentIntentResponse(paymentIntent.ClientSecret));
    }

    [HttpPost("/api/bookings/{id:int}/confirm-payment")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<RoomBookingResponseDto>> ConfirmBookingPayment(
        int id,
        [FromBody] ConfirmBookingPaymentRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var booking = await _db.RoomBookings
            .Include(b => b.Room)
                .ThenInclude(r => r!.GymLocation)
                    .ThenInclude(gl => gl!.Gym)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound();

        if (!User.IsInRole("Admin") && booking.TrainerId != actorId.Trim())
            return Forbid();

        // Already confirmed (idempotent)
        if (booking.Status == RoomBookingStatus.Confirmed)
            return Ok(ToBookingDto(booking));

        if (booking.Status != RoomBookingStatus.Pending)
            return BadRequest(new { message = "Only pending bookings can be confirmed." });

        if (string.IsNullOrWhiteSpace(request.StripePaymentIntentId))
            return BadRequest(new { message = "StripePaymentIntentId is required." });

        // Verify payment with Stripe
        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.GetAsync(request.StripePaymentIntentId.Trim());

        if (!string.Equals(paymentIntent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Payment not confirmed by Stripe." });

        var expectedAmount = Convert.ToInt64(decimal.Round(booking.TotalCost * 100m, 0, MidpointRounding.AwayFromZero));
        if (paymentIntent.AmountReceived < expectedAmount)
            return BadRequest(new { message = "Payment amount insufficient." });

        booking.Status = RoomBookingStatus.Confirmed;
        booking.StripePaymentIntentId = paymentIntent.Id;
        booking.PaidAmount = booking.TotalCost;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify queued students that the trainer has paid
        await _queueService.NotifyQueuedUsersAsync(id);

        return Ok(ToBookingDto(booking));
    }

    private static RoomBookingResponseDto ToBookingDto(RoomBooking booking) => new(
        booking.Id,
        booking.RoomId,
        booking.TrainerId,
        booking.Modality,
        booking.StartTime,
        booking.EndTime,
        booking.Status.ToString(),
        booking.TotalCost,
        booking.Notes,
        booking.CreatedAt,
        booking.UpdatedAt,
        booking.Room?.Name,
        booking.Room?.GymLocation?.Gym?.Name,
        booking.Room?.GymLocation?.Name
    );

    private async Task<ActionResult?> EnsureCanManageGymAsync(int gymId)
    {
        if (User.IsInRole("Admin"))
            return null;

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var canManageGym = await _db.Gyms.AsNoTracking()
            .AnyAsync(g => g.Id == gymId && g.OwnerUserId == actorId);

        if (!canManageGym)
            return Forbid();

        return null;
    }
}

