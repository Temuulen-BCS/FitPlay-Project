namespace FitPlay.Domain.Model
{
    public class Level
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "Beginner", "Intermediate", "Advanced", "Elite"
        public int RequiredXp { get; set; }
        public string Description { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
