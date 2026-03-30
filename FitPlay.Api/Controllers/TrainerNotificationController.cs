using System.Security.Claims;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitPlay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/trainer-notifications")]
public class TrainerNotificationController : ControllerBase
{
    private readonly ITrainerNotificationService _notificationService;

    public TrainerNotificationController(ITrainerNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    [Authorize(Roles = "GymAdmin")]
    public async Task<ActionResult<TrainerNotificationDto>> CreateNotification([FromBody] CreateTrainerNotificationRequest request)
    {
        var senderAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(senderAdminId))
            return Unauthorized();

        try
        {
            var notification = await _notificationService.CreateAsync(senderAdminId, request);
            return Ok(notification);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<List<TrainerNotificationDto>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
        var trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(trainerId))
            return Unauthorized();

        try
        {
            var notifications = await _notificationService.GetByTrainerAsync(trainerId, unreadOnly);
            return Ok(notifications);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/read")]
    [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<TrainerNotificationDto>> MarkAsRead(int id)
    {
        var trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(trainerId))
            return Unauthorized();

        try
        {
            var notification = await _notificationService.MarkAsReadAsync(id, trainerId);
            return Ok(notification);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("unread-count")]
    [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(trainerId))
            return Unauthorized();

        try
        {
            var count = await _notificationService.GetUnreadCountAsync(trainerId);
            return Ok(count);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}