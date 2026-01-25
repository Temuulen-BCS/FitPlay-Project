namespace FitPlay.Domain.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public UserRole Role { get; set; } = UserRole.Athlete;
        public int Points { get; set; }
        public int Xp { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int? BoxId { get; set; }
        public int? LevelId { get; set; }

        // Navigation properties
        public Box? Box { get; set; }
        public Level? Level { get; set; }
        public ICollection<Training> TrainingsAsTrainer { get; set; } = new List<Training>();
        public ICollection<Training> TrainingsAsAthlete { get; set; } = new List<Training>();
    }

    public enum UserRole
    {
        Athlete,
        Trainer,
        Admin
    }
}
    }
}
