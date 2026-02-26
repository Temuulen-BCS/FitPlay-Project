using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public enum ClassScheduleStatus
{
    Scheduled,
    Cancelled,
    Completed
}

public class ClassSchedule
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Modality { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }
    public ClassScheduleStatus Status { get; set; } = ClassScheduleStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
