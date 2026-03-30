using System.Security.Claims;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/trainers")]
[Authorize(Roles = "Admin,Trainer")]
public class TrainersController : ControllerBase
{
    private readonly IClassSessionService _classSessionService;
    private readonly IPaymentService _paymentService;

    public TrainersController(
        IClassSessionService classSessionService,
        IPaymentService paymentService)
    {
        _classSessionService = classSessionService;
        _paymentService = paymentService;
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
