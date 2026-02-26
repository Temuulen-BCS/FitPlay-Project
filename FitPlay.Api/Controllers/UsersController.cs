using FitPlay.Api.DTOs;
using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly FitPlayContext _db;
    public UsersController(FitPlayContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> GetAll()
    {
        var users = await _db.Users.AsNoTracking().ToListAsync();
        var dtos = users.Select(u => new UserReadDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Phone = u.Phone,
            IdentityUserId = u.IdentityUserId
        });
        return Ok(dtos);
    }

    [HttpGet("by-identity/{identityUserId}")]
    public async Task<ActionResult<UserReadDto>> GetByIdentity(string identityUserId)
    {
        var normalizedId = identityUserId.Trim();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId != null && u.IdentityUserId == normalizedId);
        if (user is null) return NotFound();

        var dto = new UserReadDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            IdentityUserId = user.IdentityUserId
        };
        return Ok(dto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserReadDto>> GetById(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var dto = new UserReadDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            IdentityUserId = user.IdentityUserId
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<UserReadDto>> Create([FromBody] UserCreateDto dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            IdentityUserId = dto.IdentityUserId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var readDto = new UserReadDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            IdentityUserId = user.IdentityUserId
        };

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, readDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserCreateDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Phone = dto.Phone;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
