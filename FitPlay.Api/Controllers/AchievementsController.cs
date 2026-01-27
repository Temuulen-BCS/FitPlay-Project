using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AchievementsController : ControllerBase
{
    private readonly AchievementService _achievementService;

    public AchievementsController(AchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    /// <summary>
    /// Get user's earned achievements.
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<AchievementDto>>> GetUserAchievements(int userId)
    {
        var achievements = await _achievementService.GetUserAchievementsAsync(userId);
        return Ok(achievements);
    }

    /// <summary>
    /// Get all achievements with earned status for a user.
    /// </summary>
    [HttpGet("user/{userId}/all")]
    public async Task<ActionResult<object>> GetAllAchievementsStatus(int userId)
    {
        var achievements = await _achievementService.GetAllAchievementsStatusAsync(userId);
        
        var result = achievements.Select(a => new
        {
            type = a.Type,
            name = a.Name,
            description = a.Description,
            earned = a.Earned != null,
            earnedAt = a.Earned?.AwardedAt
        });

        return Ok(result);
    }
}
