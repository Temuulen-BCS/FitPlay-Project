using FitPlay.Api.DTOs;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly FitPlayContext _db;
    public TeachersController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherReadDto>>> GetAll()
    {
        var teachers = await _db.Teachers.AsNoTracking().ToListAsync();
        var dtos = teachers.Select(t => new TeacherReadDto
        {
            Id = t.Id,
            Name = t.Name,
            Email = t.Email,
            Phone = t.Phone
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TeacherReadDto>> GetById(int id)
    {
        var teacher = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (teacher is null) return NotFound();

        var dto = new TeacherReadDto
        {
            Id = teacher.Id,
            Name = teacher.Name,
            Email = teacher.Email,
            Phone = teacher.Phone
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<TeacherReadDto>> Create([FromBody] TeacherCreateDto dto)
    {
        var teacher = new Teacher
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone
        };

        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var readDto = new TeacherReadDto
        {
            Id = teacher.Id,
            Name = teacher.Name,
            Email = teacher.Email,
            Phone = teacher.Phone
        };

        return CreatedAtAction(nameof(GetById), new { id = teacher.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TeacherCreateDto dto)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null) return NotFound();

        teacher.Name = dto.Name;
        teacher.Email = dto.Email;
        teacher.Phone = dto.Phone;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null) return NotFound();

        _db.Teachers.Remove(teacher);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
