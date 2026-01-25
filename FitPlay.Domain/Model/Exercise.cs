namespace FitPlay.Domain.Model
{
    public class Exercise
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public TrainingType TrainingType { get; set; }
        public string MuscleGroup { get; set; } = string.Empty; // e.g., "Chest", "Back", "Legs"
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<TrainingExercise> TrainingExercises { get; set; } = new List<TrainingExercise>();
    }

    public enum TrainingType
    {
        Bodybuilding,
        Crossfit,
        Yoga,
        Gymnastics,
        Cardio,
        Functional
    }
}
