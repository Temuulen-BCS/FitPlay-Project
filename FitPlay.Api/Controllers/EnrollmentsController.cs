using System.Security.Claims;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IClassSessionService _classSessionService;
    private readonly ICheckInService _checkInService;

    public EnrollmentsController(
        IClassSessionService classSessionService,
        ICheckInService checkInService)
    {
        _classSessionService = classSessionService;
        _checkInService = checkInService;
    }

    [HttpGet("/api/enrollments/mine")]
    public async Task<ActionResult<List<UserEnrollmentWithSessionDto>>> GetMyEnrollments()
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var enrollments = await _classSessionService.GetMyEnrollmentsAsync(actorId);
        return Ok(enrollments);
    }

    [HttpDelete("/api/enrollments/{id:int}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> CancelEnrollment(int id)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var cancelled = await _classSessionService.CancelEnrollmentAsync(id, actorId, User.IsInRole("Admin"));
            if (!cancelled) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("/api/enrollments/{id:int}/checkin")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<RoomCheckInResponseDto>> CheckIn(int id, [FromBody] CreateRoomCheckInRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var checkIn = await _checkInService.CheckInAsync(id, actorId, request);
            return Ok(checkIn);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
