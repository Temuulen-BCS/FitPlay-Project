using FitPlay.Api.DTOs;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LevelsController : ControllerBase
{
    private readonly FitPlayContext _db;
    public LevelsController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LevelReadDto>>> GetAll()
    {
        var levels = await _db.Levels.AsNoTracking().ToListAsync();
        var dtos = levels.Select(l => new LevelReadDto
        {
            Id = l.Id,
            Name = l.Name,
            ExperiencePoints = l.ExperiencePoints
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LevelReadDto>> GetById(int id)
    {
        var level = await _db.Levels.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (level is null) return NotFound();

        var dto = new LevelReadDto
        {
            Id = level.Id,
            Name = level.Name,
            ExperiencePoints = level.ExperiencePoints
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<LevelReadDto>> Create([FromBody] LevelCreateDto dto)
    {
        var level = new Level
        {
            Name = dto.Name,
            ExperiencePoints = dto.ExperiencePoints
        };

        _db.Levels.Add(level);
        await _db.SaveChangesAsync();

        var readDto = new LevelReadDto
        {
            Id = level.Id,
            Name = level.Name,
            ExperiencePoints = level.ExperiencePoints
        };

        return CreatedAtAction(nameof(GetById), new { id = level.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LevelCreateDto dto)
    {
        var level = await _db.Levels.FindAsync(id);
        if (level is null) return NotFound();

        level.Name = dto.Name;
        level.ExperiencePoints = dto.ExperiencePoints;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var level = await _db.Levels.FindAsync(id);
        if (level is null) return NotFound();

        _db.Levels.Remove(level);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
