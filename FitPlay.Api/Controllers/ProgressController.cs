using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgressController : ControllerBase
{
    private readonly ProgressService _progressService;

    public ProgressController(ProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>
    /// Get user's progress (XP, level, streak).
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserProgressDto>> GetProgress(int userId)
    {
        var progress = await _progressService.GetUserProgressAsync(userId);
        return Ok(progress);
    }

    /// <summary>
    /// Get user's XP transaction history.
    /// </summary>
    [HttpGet("{userId}/history")]
    public async Task<ActionResult<List<XpTransactionDto>>> GetXpHistory(int userId, [FromQuery] int limit = 50)
    {
        var history = await _progressService.GetXpHistoryAsync(userId, limit);
        return Ok(history);
    }

    /// <summary>
    /// Award bonus XP to a user (trainer action).
    /// </summary>
    [HttpPost("bonus")]
    // [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<UserProgressDto>> AwardBonusXp([FromBody] AwardBonusXpRequest request, [FromQuery] int trainerId)
    {
        var (newTotalXp, newLevel, leveledUp) = await _progressService.AddXpAsync(
            request.UserId,
            request.XpAmount,
            Domain.Models.XpTransactionType.BonusFromTrainer,
            reason: request.Reason,
            awardedByTrainerId: trainerId
        );

        var progress = await _progressService.GetUserProgressAsync(request.UserId);
        return Ok(progress);
    }

    /// <summary>
    /// Reset a user's XP (trainer action).
    /// </summary>
    [HttpPost("reset")]
    // [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<UserProgressDto>> ResetXp([FromBody] ResetXpRequest request, [FromQuery] int trainerId)
    {
        await _progressService.ResetXpAsync(request.UserId, request.NewXpValue, request.Reason, trainerId);
        var progress = await _progressService.GetUserProgressAsync(request.UserId);
        return Ok(progress);
    }
}
