using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/classeschedules")]
public class ClassSchedulesController : ControllerBase
{
    private readonly ClassScheduleService _scheduleService;

    public ClassSchedulesController(ClassScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<List<ClassScheduleDto>>> GetUserSchedule(
        int userId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _scheduleService.GetUserScheduleAsync(userId, from, to);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClassScheduleDto>> GetById(int id)
    {
        var result = await _scheduleService.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ClassScheduleDto>> Create([FromBody] CreateClassScheduleRequest request)
    {
        var result = await _scheduleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClassScheduleDto>> Update(int id, [FromBody] UpdateClassScheduleRequest request)
    {
        var result = await _scheduleService.UpdateAsync(id, request);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _scheduleService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
