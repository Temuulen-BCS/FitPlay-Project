using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitPlay.Domain.Models;

public class TeacherProfile
{
    public int Id { get; set; }
    public int UserId { get; set; } 
    public string Bio { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
