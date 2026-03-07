namespace FitPlay.Domain.Models;

public enum RoomBookingPurpose
{
    PersonalTraining,
    GroupClass,
    Rehabilitation,
    Assessment,
    Yoga,
    Pilates,
    Crossfit,
    Other
}

public enum RoomBookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public class RoomBooking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public RoomBookingPurpose Purpose { get; set; }
    public string? PurposeDescription { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public RoomBookingStatus Status { get; set; } = RoomBookingStatus.Pending;
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Room? Room { get; set; }
    public ClassSession? ClassSession { get; set; }
}
