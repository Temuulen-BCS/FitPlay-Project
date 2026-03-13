using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/academies")]
[Authorize]
public class AcademiesController : ControllerBase
{
    private readonly IAcademyService _academyService;
    private readonly IClassSessionService _classSessionService;

    public AcademiesController(IAcademyService academyService, IClassSessionService classSessionService)
    {
        _academyService = academyService;
        _classSessionService = classSessionService;
    }

    // ── Gyms ──────────────────────────────────────────────────────────────────

    [HttpGet("mine")]
    [Authorize(Roles = "GymAdmin")]
    public async Task<ActionResult<GymResponseDto>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var gym = await _academyService.GetMyGymAsync(userId);
        if (gym is null) return NotFound();
        return Ok(gym);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Trainer,GymAdmin")]
    public async Task<ActionResult<List<GymResponseDto>>> GetAll([FromQuery] bool? isActive = null)
    {
        var gyms = await _academyService.GetGymsAsync(isActive);
        return Ok(gyms);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<GymResponseDto>> GetById(int id)
    {
        var gym = await _academyService.GetGymByIdAsync(id);
        if (gym is null) return NotFound();
        return Ok(gym);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<GymResponseDto>> Create([FromBody] CreateGymRequest request)
    {
        var ownerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var gym = await _academyService.CreateGymAsync(request, ownerUserId);
        return CreatedAtAction(nameof(GetById), new { id = gym.Id }, gym);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<GymResponseDto>> Update(int id, [FromBody] UpdateGymRequest request)
    {
        var gym = await _academyService.UpdateGymAsync(id, request);
        if (gym is null) return NotFound();
        return Ok(gym);
    }

    // ── Locations ─────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/locations")]
    [Authorize(Roles = "Admin,Trainer,GymAdmin")]
    public async Task<ActionResult<List<GymLocationResponseDto>>> GetLocations(int id, [FromQuery] bool? isActive = null)
    {
        var locations = await _academyService.GetGymLocationsAsync(id, isActive);
        return Ok(locations);
    }

    [HttpPost("{id:int}/locations")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<GymLocationResponseDto>> CreateLocation(int id, [FromBody] CreateGymLocationRequest request)
    {
        // Override GymId from route to prevent mismatch
        var req = request with { GymId = id };
        var location = await _academyService.CreateGymLocationAsync(req);
        return Created($"/api/academies/{id}/locations/{location.Id}", location);
    }

    [HttpDelete("{gymId:int}/locations/{locationId:int}")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<IActionResult> DeleteLocation(int gymId, int locationId)
    {
        var deleted = await _academyService.DeleteGymLocationAsync(gymId, locationId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    // ── Trainer links ─────────────────────────────────────────────────────────

    [HttpGet("{id:int}/trainers")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<List<TrainerGymLinkResponseDto>>> GetTrainers(int id)
    {
        var links = await _academyService.GetGymTrainerLinksAsync(id);
        return Ok(links);
    }

    [HttpPost("{id:int}/link-trainer")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<TrainerGymLinkResponseDto>> LinkTrainer(int id, [FromBody] CreateTrainerGymLinkRequest request)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid gym id." });

        var created = await _academyService.LinkTrainerAsync(new CreateTrainerGymLinkRequest(
            request.TrainerId,
            id
        ));

        return Ok(created);
    }

    [HttpPatch("{gymId:int}/trainers/{linkId:int}/status")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<TrainerGymLinkResponseDto>> UpdateTrainerStatus(
        int gymId, int linkId, [FromBody] UpdateTrainerGymLinkStatusRequest request)
    {
        try
        {
            var updated = await _academyService.UpdateTrainerLinkStatusAsync(linkId, request);
            if (updated is null) return NotFound();
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Sessions ──────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/sessions")]
    [Authorize(Roles = "Admin,GymAdmin")]
    public async Task<ActionResult<List<FitPlay.Domain.DTOs.ClassSessionResponseDto>>> GetSessions(
        int id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var sessions = await _classSessionService.GetSessionsByGymAsync(id, from, to);
        return Ok(sessions);
    }
}
