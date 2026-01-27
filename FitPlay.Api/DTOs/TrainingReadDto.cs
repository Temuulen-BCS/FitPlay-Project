namespace FitPlay.Api.DTOs;

public class TrainingReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMin { get; set; }
    public int Points { get; set; }
    public string Athletes { get; set; } = string.Empty;
}
