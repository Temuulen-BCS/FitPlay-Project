using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/academies")]
[Authorize]
public class AcademiesController : ControllerBase
{
    private readonly IAcademyService _academyService;

    public AcademiesController(IAcademyService academyService)
    {
        _academyService = academyService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<List<GymResponseDto>>> GetAll([FromQuery] bool? isActive = null)
    {
        var gyms = await _academyService.GetGymsAsync(isActive);
        return Ok(gyms);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GymResponseDto>> Create([FromBody] CreateGymRequest request)
    {
        var gym = await _academyService.CreateGymAsync(request);
        return CreatedAtAction(nameof(GetLocations), new { id = gym.Id }, gym);
    }

    [HttpPost("{id:int}/link-trainer")]
    [Authorize(Roles = "Admin")]
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

    [HttpGet("{id:int}/locations")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<ActionResult<List<GymLocationResponseDto>>> GetLocations(int id, [FromQuery] bool? isActive = null)
    {
        var locations = await _academyService.GetGymLocationsAsync(id, isActive);
        return Ok(locations);
    }
}
