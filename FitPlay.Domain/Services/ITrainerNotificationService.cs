using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface ITrainerNotificationService
{
    Task<TrainerNotificationDto> CreateAsync(string senderAdminId, CreateTrainerNotificationRequest request);
    Task<List<TrainerNotificationDto>> GetByTrainerAsync(string trainerId, bool unreadOnly = false);
    Task<TrainerNotificationDto> MarkAsReadAsync(int notificationId, string trainerId);
    Task<int> GetUnreadCountAsync(string trainerId);
}