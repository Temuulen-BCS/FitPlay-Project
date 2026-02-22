using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitPlay.Domain.Models;

public class ExerciseLog
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int ExerciseId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public int DurationMin { get; set; }
    public int PointsAwarded { get; set; }
    public string? Notes { get; set; }
}
