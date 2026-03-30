using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingsController : ControllerBase
{
    private readonly TrainingService _trainingService;
    public TrainingsController(TrainingService trainingService) => _trainingService = trainingService;

    [HttpGet]
    public async Task<ActionResult<List<TrainingSummaryDto>>> GetAll([FromQuery] int? userId = null)
    {
        var trainings = await _trainingService.GetTrainingsAsync(userId);
        return Ok(trainings);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingDto>> GetById(int id)
    {
        var training = await _trainingService.GetTrainingAsync(id);
        if (training is null) return NotFound();
        return Ok(training);
    }

    [HttpPost]
    public async Task<ActionResult<TrainingDto>> Create([FromBody] CreateTrainingRequest request, [FromQuery] int trainerId)
    {
        var training = await _trainingService.CreateTrainingAsync(trainerId, request);
        return CreatedAtAction(nameof(GetById), new { id = training.Id }, training);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TrainingDto>> Update(int id, [FromBody] UpdateTrainingRequest request, [FromQuery] int trainerId)
    {
        var training = await _trainingService.UpdateTrainingAsync(id, trainerId, request);
        if (training is null) return NotFound();
        return Ok(training);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int trainerId)
    {
        var success = await _trainingService.DeleteTrainingAsync(id, trainerId);
        if (!success) return NotFound();
        return NoContent();
    }
}
