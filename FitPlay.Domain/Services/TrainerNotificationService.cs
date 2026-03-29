using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class TrainerNotificationService : ITrainerNotificationService
{
    private readonly FitPlayContext _db;
    private readonly IClockService _clock;

    public TrainerNotificationService(FitPlayContext db, IClockService clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<TrainerNotificationDto> CreateAsync(string senderAdminId, CreateTrainerNotificationRequest request)
    {
        var notification = new TrainerNotification
        {
            TrainerId = request.TrainerId,
            SenderGymAdminId = senderAdminId,
            GymLocationId = request.GymLocationId,
            SubjectUserId = request.SubjectUserId,
            Message = request.Message,
            IsRead = false,
            CreatedAt = _clock.UtcNow
        };

        _db.TrainerNotifications.Add(notification);
        await _db.SaveChangesAsync();

        // Load the GymLocation for the DTO
        var gymLocation = await _db.GymLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(gl => gl.Id == notification.GymLocationId);

        return new TrainerNotificationDto(
            notification.Id,
            notification.TrainerId,
            notification.SenderGymAdminId,
            notification.GymLocationId,
            gymLocation?.Name ?? string.Empty,
            notification.SubjectUserId,
            string.Empty, // SubjectUserName - will be populated in controller
            notification.Message,
            notification.IsRead,
            notification.CreatedAt
        );
    }

    public async Task<List<TrainerNotificationDto>> GetByTrainerAsync(string trainerId, bool unreadOnly = false)
    {
        var query = _db.TrainerNotifications
            .Include(tn => tn.GymLocation)
            .Where(tn => tn.TrainerId == trainerId);

        if (unreadOnly)
        {
            query = query.Where(tn => !tn.IsRead);
        }

        var notifications = await query
            .OrderByDescending(tn => tn.CreatedAt)
            .ToListAsync();

        return notifications.Select(tn => new TrainerNotificationDto(
            tn.Id,
            tn.TrainerId,
            tn.SenderGymAdminId,
            tn.GymLocationId,
            tn.GymLocation?.Name ?? string.Empty,
            tn.SubjectUserId,
            string.Empty, // SubjectUserName - will be populated in controller
            tn.Message,
            tn.IsRead,
            tn.CreatedAt
        )).ToList();
    }

    public async Task<TrainerNotificationDto> MarkAsReadAsync(int notificationId, string trainerId)
    {
        var notification = await _db.TrainerNotifications
            .Include(tn => tn.GymLocation)
            .FirstOrDefaultAsync(tn => tn.Id == notificationId && tn.TrainerId == trainerId);

        if (notification == null)
            throw new ArgumentException("Notification not found or you don't have permission to access it.");

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return new TrainerNotificationDto(
            notification.Id,
            notification.TrainerId,
            notification.SenderGymAdminId,
            notification.GymLocationId,
            notification.GymLocation?.Name ?? string.Empty,
            notification.SubjectUserId,
            string.Empty, // SubjectUserName - will be populated in controller
            notification.Message,
            notification.IsRead,
            notification.CreatedAt
        );
    }

    public async Task<int> GetUnreadCountAsync(string trainerId)
    {
        return await _db.TrainerNotifications
            .CountAsync(tn => tn.TrainerId == trainerId && !tn.IsRead);
    }
}