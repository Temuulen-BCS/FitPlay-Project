using FitPlay.Api.DTOs;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingsController : ControllerBase
{
    private readonly FitPlayContext _db;
    public RankingsController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RankingReadDto>>> GetAll()
    {
        var rankings = await _db.Rankings.AsNoTracking().ToListAsync();
        var dtos = rankings.Select(r => new RankingReadDto
        {
            Id = r.Id,
            User = r.User,
            Description = r.Description,
            Points = r.Points
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RankingReadDto>> GetById(int id)
    {
        var ranking = await _db.Rankings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (ranking is null) return NotFound();

        var dto = new RankingReadDto
        {
            Id = ranking.Id,
            User = ranking.User,
            Description = ranking.Description,
            Points = ranking.Points
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<RankingReadDto>> Create([FromBody] RankingCreateDto dto)
    {
        var ranking = new Ranking
        {
            User = dto.User,
            Description = dto.Description,
            Points = dto.Points
        };

        _db.Rankings.Add(ranking);
        await _db.SaveChangesAsync();

        var readDto = new RankingReadDto
        {
            Id = ranking.Id,
            User = ranking.User,
            Description = ranking.Description,
            Points = ranking.Points
        };

        return CreatedAtAction(nameof(GetById), new { id = ranking.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RankingCreateDto dto)
    {
        var ranking = await _db.Rankings.FindAsync(id);
        if (ranking is null) return NotFound();

        ranking.User = dto.User;
        ranking.Description = dto.Description;
        ranking.Points = dto.Points;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ranking = await _db.Rankings.FindAsync(id);
        if (ranking is null) return NotFound();

        _db.Rankings.Remove(ranking);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
