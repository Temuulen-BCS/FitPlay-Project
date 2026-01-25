namespace FitPlay.Domain.Model
{
    public class Box
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Training> Trainings { get; set; } = new List<Training>();
    }
}
