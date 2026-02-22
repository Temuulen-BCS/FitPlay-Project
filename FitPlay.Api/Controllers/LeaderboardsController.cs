using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitPlay.Domain.Data;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardsController : ControllerBase
{
    private readonly FitPlayContext _db;
    public LeaderboardsController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string period = "week", [FromQuery] int? teacherId = null)
    {
        DateTime from = period.ToLower() switch
        {
            "month" => DateTime.UtcNow.AddDays(-30),
            "all" => DateTime.MinValue,
            _ => DateTime.UtcNow.AddDays(-7)
        };

        var query = _db.ExerciseLogs.AsNoTracking().Where(l => l.PerformedAt >= from);
        if (teacherId.HasValue)
        {
            var exIds = await _db.Exercises.Where(e => e.TeacherId == teacherId.Value).Select(e => e.Id).ToListAsync();
            query = query.Where(l => exIds.Contains(l.ExerciseId));
        }

        var data = await query
            .GroupBy(l => l.ClientId)
            .Select(g => new { ClientId = g.Key, Points = g.Sum(x => x.PointsAwarded) })
            .OrderByDescending(x => x.Points)
            .Take(100)
            .ToListAsync();

        return Ok(data);
    }
}
