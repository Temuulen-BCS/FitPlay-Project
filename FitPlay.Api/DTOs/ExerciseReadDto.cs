namespace FitPlay.Api.DTOs;

public class ExerciseReadDto
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public int BasePoints { get; set; }
    public int SuggestedDurationMin { get; set; }
    public bool IsActive { get; set; }
}
