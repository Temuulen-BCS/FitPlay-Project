namespace FitPlay.Domain.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public MyTraining Training { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Point { get; set; }
        public string Level { get; set; } = string.Empty;
        public int Xp { get; set; } 
        public DateOnly? BirthDate { get; set; }

        public enum MyTraining
        {
            Bodybuilding,
            Yoga,
            Crossfit,
            Gymnastics
        }
    }
}
