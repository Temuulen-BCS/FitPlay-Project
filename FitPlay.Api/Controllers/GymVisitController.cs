using System.Security.Claims;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gym-visits")]
public class GymVisitController : ControllerBase
{
    private readonly IGymVisitService _gymVisitService;

    public GymVisitController(IGymVisitService gymVisitService)
    {
        _gymVisitService = gymVisitService;
    }

    [HttpPost("checkin")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<GymVisitResponseDto>> CheckIn([FromBody] GymCheckInRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var visit = await _gymVisitService.CheckInAsync(actorId, request);
            return Ok(visit);
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

    [HttpPost("checkout")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<GymVisitResponseDto>> CheckOut([FromBody] GymCheckOutRequest request)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        try
        {
            var visit = await _gymVisitService.CheckOutAsync(actorId, request);
            return Ok(visit);
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

    [HttpGet("active")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<GymVisitResponseDto>> GetActiveVisit()
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var activeVisit = await _gymVisitService.GetActiveVisitAsync(actorId);
        
        if (activeVisit == null)
            return NoContent();

        return Ok(activeVisit);
    }

    [HttpGet("history")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<List<GymVisitResponseDto>>> GetVisitHistory([FromQuery] int limit = 50)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return Unauthorized();

        var history = await _gymVisitService.GetVisitHistoryAsync(actorId, limit);
        return Ok(history);
    }
}

[ApiController]
[Route("api/gym-locations")]
public class GymLocationController : ControllerBase
{
    private readonly IGymVisitService _gymVisitService;

    public GymLocationController(IGymVisitService gymVisitService)
    {
        _gymVisitService = gymVisitService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<GymLocationForCheckInDto>>> GetAllActiveLocations()
    {
        var locations = await _gymVisitService.GetAllActiveGymLocationsAsync();
        return Ok(locations);
    }
}