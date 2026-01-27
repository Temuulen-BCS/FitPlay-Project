using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingCompletionsController : ControllerBase
{
    private readonly TrainingCompletionService _completionService;

    public TrainingCompletionsController(TrainingCompletionService completionService)
    {
        _completionService = completionService;
    }

    /// <summary>
    /// Complete a training (user action).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CompleteTrainingResponse>> CompleteTraining(
        [FromBody] CompleteTrainingRequest request,
        [FromQuery] int userId)
    {
        try
        {
            var result = await _completionService.CompleteTrainingAsync(userId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's training completions.
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<TrainingCompletionDto>>> GetUserCompletions(int userId, [FromQuery] int limit = 50)
    {
        var completions = await _completionService.GetUserCompletionsAsync(userId, limit);
        return Ok(completions);
    }

    /// <summary>
    /// Get pending validations for a trainer.
    /// </summary>
    [HttpGet("pending/{trainerId}")]
    // [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<List<TrainingCompletionDto>>> GetPendingValidations(int trainerId)
    {
        var pending = await _completionService.GetPendingValidationsAsync(trainerId);
        return Ok(pending);
    }

    /// <summary>
    /// Validate a training completion (trainer action).
    /// </summary>
    [HttpPost("validate")]
    // [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<CompleteTrainingResponse>> ValidateCompletion(
        [FromBody] ValidateCompletionRequest request,
        [FromQuery] int trainerId)
    {
        try
        {
            var result = await _completionService.ValidateCompletionAsync(trainerId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
