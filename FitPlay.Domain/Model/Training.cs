namespace FitPlay.Domain.Model
{
    public class Training
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TrainingType Type { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
        public int Points { get; set; }
        public int MaxAthletes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int? BoxId { get; set; }
        public int? TrainerId { get; set; }

        // Navigation properties
        public Box? Box { get; set; }
        public User? Trainer { get; set; }
        public ICollection<User> Athletes { get; set; } = new List<User>();
        public ICollection<TrainingExercise> TrainingExercises { get; set; } = new List<TrainingExercise>();
    }
}
