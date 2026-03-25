using System.Security.Claims;
using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/trainers")]
[Authorize(Roles = "Admin,Trainer")]
public class TrainersController : ControllerBase
{
    private readonly IClassSessionService _classSessionService;
    private readonly IPaymentService _paymentService;
    private readonly FitPlayContext _db;

    public TrainersController(
        IClassSessionService classSessionService,
        IPaymentService paymentService,
        FitPlayContext db)
    {
        _classSessionService = classSessionService;
        _paymentService = paymentService;
        _db = db;
    }

    [HttpGet("{id}/sessions")]
    public async Task<ActionResult<List<ClassSessionResponseDto>>> GetTrainerSessions(
        string id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!CanAccessTrainerResource(id))
            return Forbid();

        var sessions = await _classSessionService.GetTrainerSessionsAsync(id, from, to);
        return Ok(sessions);
    }

    [HttpGet("{id}/completed-schedules")]
    public async Task<ActionResult<List<ClassSessionResponseDto>>> GetTrainerCompletedSchedules(string id)
    {
        if (!CanAccessTrainerResource(id))
            return Forbid();

        // Bridge identity ID → Teacher int ID
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.IdentityUserId == id);
        if (teacher is null)
            return Ok(new List<ClassSessionResponseDto>());

        var schedules = await _db.ClassSchedules
            .Where(s => s.TrainerId == teacher.Id && s.Status == ClassScheduleStatus.Completed)
            .Include(s => s.User)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();

        var result = schedules.Select(s => new ClassSessionResponseDto(
            Id: s.Id,
            RoomBookingId: s.RoomBookingId ?? 0,
            TrainerId: id,
            Title: s.Modality,
            Description: s.Notes,
            MaxStudents: 1,
            PricePerStudent: s.PaidAmount ?? 0m,
            StartTime: s.ScheduledAt,
            EndTime: s.ScheduledAt,
            Status: s.Status.ToString(),
            EnrolledStudents: s.UserId.HasValue ? 1 : 0
        )).ToList();

        return Ok(result);
    }

    [HttpGet("{id}/earnings")]
    public async Task<ActionResult<TrainerEarningsSummaryDto>> GetTrainerEarnings(
        string id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!CanAccessTrainerResource(id))
            return Forbid();

        var earnings = await _paymentService.GetTrainerEarningsAsync(id, from, to);
        return Ok(earnings);
    }

    private bool CanAccessTrainerResource(string trainerId)
    {
        if (User.IsInRole("Admin"))
            return true;

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
            return false;

        return string.Equals(actorId, trainerId, StringComparison.Ordinal);
    }
}
