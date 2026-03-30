using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.Models;

public enum ClassSessionStatus
{
    Scheduled,
    Ongoing,
    Completed,
    Cancelled
}

public class ClassSession
{
    public int Id { get; set; }
    public int RoomBookingId { get; set; }
    public string TrainerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int MaxStudents { get; set; }
    public decimal PricePerStudent { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ClassSessionStatus Status { get; set; } = ClassSessionStatus.Scheduled;

    public RoomBooking? RoomBooking { get; set; }
    public List<ClassEnrollment> Enrollments { get; set; } = new();
}
