using FitPlay.Api.DTOs;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingsController : ControllerBase
{
    private readonly FitPlayContext _db;
    public TrainingsController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrainingReadDto>>> GetAll()
    {
        var trainings = await _db.Trainings.AsNoTracking().ToListAsync();
        var dtos = trainings.Select(t => new TrainingReadDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            DurationMin = t.DurationMin,
            Points = t.Points,
            Athletes = t.Athletes
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingReadDto>> GetById(int id)
    {
        var training = await _db.Trainings.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (training is null) return NotFound();

        var dto = new TrainingReadDto
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description,
            DurationMin = training.DurationMin,
            Points = training.Points,
            Athletes = training.Athletes
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<TrainingReadDto>> Create([FromBody] TrainingCreateDto dto)
    {
        var training = new Training
        {
            Name = dto.Name,
            Description = dto.Description,
            DurationMin = dto.DurationMin,
            Points = dto.Points,
            Athletes = dto.Athletes
        };

        _db.Trainings.Add(training);
        await _db.SaveChangesAsync();

        var readDto = new TrainingReadDto
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description,
            DurationMin = training.DurationMin,
            Points = training.Points,
            Athletes = training.Athletes
        };

        return CreatedAtAction(nameof(GetById), new { id = training.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TrainingCreateDto dto)
    {
        var training = await _db.Trainings.FindAsync(id);
        if (training is null) return NotFound();

        training.Name = dto.Name;
        training.Description = dto.Description;
        training.DurationMin = dto.DurationMin;
        training.Points = dto.Points;
        training.Athletes = dto.Athletes;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var training = await _db.Trainings.FindAsync(id);
        if (training is null) return NotFound();

        _db.Trainings.Remove(training);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
