using System.Security.Claims;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet("/api/locations/{id:int}/rooms")]
    public async Task<ActionResult<List<RoomResponseDto>>> GetRoomsByLocation(int id, [FromQuery] bool? isActive = null)
    {
        var rooms = await _roomService.GetRoomsByLocationAsync(id, isActive);
        return Ok(rooms);
    }

    [HttpPut("/api/rooms/{id:int}")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<RoomResponseDto>> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
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
}

