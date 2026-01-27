namespace FitPlay.Domain.Models;

/// <summary>
/// Junction table linking a Training to its Exercises with order and optional overrides.
/// </summary>
public class TrainingExercise
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public int ExerciseId { get; set; }
    
    /// <summary>
    /// Order of this exercise within the training.
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Number of sets for this exercise in this training.
    /// </summary>
    public int Sets { get; set; } = 3;
    
    /// <summary>
    /// Number of reps per set.
    /// </summary>
    public int Reps { get; set; } = 10;
    
    /// <summary>
    /// Rest time between sets in seconds.
    /// </summary>
    public int RestSeconds { get; set; } = 60;
    
    /// <summary>
    /// Optional notes for this exercise in this training.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation
    public Training? Training { get; set; }
    public Exercise? Exercise { get; set; }
}
