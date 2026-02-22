using FitPlay.Api.DTOs;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExerciseLogsController : ControllerBase
{
    private readonly FitPlayContext _db;
    public ExerciseLogsController(FitPlayContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLogDto req)
    {
        var ex = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == req.ExerciseId);
        if (ex is null) return NotFound("Exercise not found");

        var awarded = PointsCalculator.Calculate(ex.BasePoints, ex.Difficulty, req.DurationMin, ex.SuggestedDurationMin);

        var log = new ExerciseLog
        {
            ClientId = req.ClientId,
            ExerciseId = req.ExerciseId,
            DurationMin = req.DurationMin,
            Notes = req.Notes,
            PointsAwarded = awarded
        };

        _db.ExerciseLogs.Add(log);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = log.Id }, log);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _db.ExerciseLogs.FindAsync(id);
        return log is null ? NotFound() : Ok(log);
    }
}
