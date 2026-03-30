using System.Security.Claims;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly IClassSessionService _classSessionService;

    public SessionsController(IClassSessionService classSessionService)
    {
        _classSessionService = classSessionService;
    }

    [HttpPost("/api/bookings/{id:int}/sessions")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<ClassSessionResponseDto>> CreateSession(int id, [FromBody] CreateClassSessionRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var session = await _classSessionService.CreateSessionAsync(id, actorId, request);
            return Created($"/api/sessions/{session.Id}", session);
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

    [HttpGet("/api/sessions/{id:int}")]
    public async Task<ActionResult<ClassSessionResponseDto>> GetSession(int id)
    {
        var session = await _classSessionService.GetSessionByIdAsync(id);
        if (session is null) return NotFound();
        return Ok(session);
    }

    [HttpPost("/api/sessions/{id:int}/enroll")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<ClassEnrollmentResponseDto>> Enroll(int id, [FromBody] CreateClassEnrollmentRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var enrollment = await _classSessionService.EnrollAsync(id, actorId, request);
            return Created($"/api/enrollments/{enrollment.Id}", enrollment);
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
    [HttpDelete("/api/sessions/{id:int}")]
    [Authorize(Roles = "Admin,GymAdmin,Trainer")]
    public async Task<IActionResult> CancelSession(int id)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var cancelled = await _classSessionService.CancelSessionAsync(id, actorId, User.IsInRole("Admin") || User.IsInRole("GymAdmin"));
            if (!cancelled) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("/api/sessions/{id:int}/enrollments")]
    [Authorize(Roles = "Admin,GymAdmin,Trainer")]
    public async Task<ActionResult<List<SessionEnrollmentDto>>> GetSessionEnrollments(int id)
    {
        var enrollments = await _classSessionService.GetEnrollmentsBySessionAsync(id);
        return Ok(enrollments);
    }

    [HttpGet("/api/sessions/{id:int}/enrollments/details")]
    [Authorize(Roles = "Admin,GymAdmin,Trainer")]
    public async Task<ActionResult<List<SessionEnrollmentDetailDto>>> GetSessionEnrollmentDetails(int id)
    {
        var enrollments = await _classSessionService.GetEnrollmentDetailsBySessionAsync(id);
        return Ok(enrollments);
    }
}

