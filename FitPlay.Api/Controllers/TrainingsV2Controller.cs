using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/v2/[controller]")]
public class TrainingsV2Controller : ControllerBase
{
    private readonly TrainingService _trainingService;

    public TrainingsV2Controller(TrainingService trainingService)
    {
        _trainingService = trainingService;
    }

    /// <summary>
    /// Get all active trainings.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TrainingSummaryDto>>> GetAll([FromQuery] int? userId = null)
    {
        var trainings = await _trainingService.GetTrainingsAsync(userId);
        return Ok(trainings);
    }

    /// <summary>
    /// Get trainings by a specific trainer.
    /// </summary>
    [HttpGet("trainer/{trainerId}")]
    public async Task<ActionResult<List<TrainingSummaryDto>>> GetByTrainer(int trainerId)
    {
        var trainings = await _trainingService.GetTrainerTrainingsAsync(trainerId);
        return Ok(trainings);
    }

    /// <summary>
    /// Get a training by ID with full details.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingDto>> GetById(int id)
    {
        var training = await _trainingService.GetTrainingAsync(id);
        if (training == null) return NotFound();
        return Ok(training);
    }

    /// <summary>
    /// Create a new training (trainer action).
    /// </summary>
    [HttpPost]
    // [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<TrainingDto>> Create([FromBody] CreateTrainingRequest request, [FromQuery] int trainerId)
    {
        var training = await _trainingService.CreateTrainingAsync(trainerId, request);
        return CreatedAtAction(nameof(GetById), new { id = training.Id }, training);
    }

    /// <summary>
    /// Update a training (trainer action).
    /// </summary>
    [HttpPut("{id:int}")]
    // [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<TrainingDto>> Update(int id, [FromBody] UpdateTrainingRequest request, [FromQuery] int trainerId)
    {
        var training = await _trainingService.UpdateTrainingAsync(id, trainerId, request);
        if (training == null) return NotFound();
        return Ok(training);
    }

    /// <summary>
    /// Delete a training (trainer action - soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    // [Authorize(Roles = "Trainer")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int trainerId)
    {
        var success = await _trainingService.DeleteTrainingAsync(id, trainerId);
        if (!success) return NotFound();
        return NoContent();
    }
}
