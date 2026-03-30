using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record TrainerNotificationDto(
    int Id,
    string TrainerId,
    string SenderGymAdminId,
    int GymLocationId,
    string GymLocationName,
    string SubjectUserId,
    string SubjectUserName,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);

public record CreateTrainerNotificationRequest(
    [Required]
    string TrainerId,
    
    [Required]
    int GymLocationId,
    
    [Required]
    string SubjectUserId,
    
    [Required]
    [MaxLength(1000)]
    string Message
);

public record MarkNotificationReadRequest(
    int NotificationId
);