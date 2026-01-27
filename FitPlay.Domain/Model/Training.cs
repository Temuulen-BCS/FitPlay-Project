namespace FitPlay.Domain.Models
{
    /// <summary>
    /// A training created by a trainer, containing multiple exercises.
    /// XP is awarded when the training is fully completed.
    /// </summary>
    public class Training
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMin { get; set; }
        
        /// <summary>
        /// Points awarded for completing this training (legacy field).
        /// </summary>
        public int Points { get; set; }
        
        /// <summary>
        /// Athletes/participants associated with this training.
        /// </summary>
        public string Athletes { get; set; } = string.Empty;
        
        /// <summary>
        /// Total XP awarded for completing this training (set by trainer).
        /// </summary>
        public int XpReward { get; set; }
        
        /// <summary>
        /// Difficulty level (1-5), affects display and optional XP modifiers.
        /// </summary>
        public int Difficulty { get; set; } = 1;
        
        /// <summary>
        /// Trainer who created this training.
        /// </summary>
        public int TrainerId { get; set; }
        
        /// <summary>
        /// Whether completion requires trainer validation.
        /// </summary>
        public bool RequiresValidation { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Teacher? Trainer { get; set; }
        public ICollection<TrainingExercise> Exercises { get; set; } = new List<TrainingExercise>();
    }
}
