using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitPlay.Api.DTOs;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly FitPlayContext _db;
    public ExercisesController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.Exercises.AsNoTracking().ToListAsync());

    // (Optional) Keep returning entity OR return DTO - see below
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.Exercises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ExerciseReadDto>> Create([FromBody] ExerciseCreateDto dto)
    {
        var exercise = new Exercise
        {
            TeacherId = dto.TeacherId,
            Title = dto.Title,
            Category = dto.Category,
            Difficulty = dto.Difficulty,
            BasePoints = dto.BasePoints,
            SuggestedDurationMin = dto.SuggestedDurationMin,
            IsActive = dto.IsActive
        };

        _db.Exercises.Add(exercise);
        await _db.SaveChangesAsync();

        var readDto = new ExerciseReadDto
        {
            Id = exercise.Id,
            TeacherId = exercise.TeacherId,
            Title = exercise.Title,
            Category = exercise.Category,
            Difficulty = exercise.Difficulty,
            BasePoints = exercise.BasePoints,
            SuggestedDurationMin = exercise.SuggestedDurationMin,
            IsActive = exercise.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = exercise.Id }, readDto);
    }


    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Exercise dto)
    {
        if (id != dto.Id) return BadRequest();
        _db.Entry(dto).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Exercises.FindAsync(id);
        if (item is null) return NotFound();
        _db.Exercises.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
