namespace FitPlay.Domain.Models;

public class RoomCheckIn
{
    public int Id { get; set; }
    public int ClassEnrollmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
    public int XpAwarded { get; set; }

    public ClassEnrollment? ClassEnrollment { get; set; }
}
