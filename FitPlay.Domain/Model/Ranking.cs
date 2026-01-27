namespace FitPlay.Domain.Models
{
    public class Ranking
    {
        public int Id { get; set; }
        public string User { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Points { get; set; }
    }
}
