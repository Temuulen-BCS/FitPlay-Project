namespace FitPlay.Domain.Models;

public enum TrainerGymLinkStatus
{
    Pending,
    Approved,
    Rejected
}

public class TrainerGymLink
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public int GymId { get; set; }
    public TrainerGymLinkStatus Status { get; set; } = TrainerGymLinkStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Gym? Gym { get; set; }
}
