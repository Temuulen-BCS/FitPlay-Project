namespace FitPlay.Domain.Model
{
    public class TrainingExercise
    {
        public int Id { get; set; }

        // Foreign Keys
        public int TrainingId { get; set; }
        public int ExerciseId { get; set; }

        // Exercise details for this specific training
        public string Group { get; set; } = "WOD"; // Warmup, WOD (Workout of the Day), Skill, Cooldown
        public int Sets { get; set; }
        public string Repetitions { get; set; } = string.Empty; // e.g., "10", "10-12", "AMRAP"
        public string Weight { get; set; } = string.Empty; // e.g., "50kg", "bodyweight", "50% 1RM"
        public string RestTime { get; set; } = string.Empty; // e.g., "60s", "2min"
        public string Notes { get; set; } = string.Empty;
        public int Order { get; set; } // Order of the exercise in the training

        // Navigation properties
        public Training Training { get; set; } = null!;
        public Exercise Exercise { get; set; } = null!;
    }
}
