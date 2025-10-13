using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitPlay.Domain.Models;

public class Exercise
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; 
    public int Difficulty { get; set; } 
    public int BasePoints { get; set; } = 100;
    public int SuggestedDurationMin { get; set; } = 20;
    public bool IsActive { get; set; } = true;
}
