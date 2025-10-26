
namespace FitPlay.Domain.model
{
    public class Game
    {
        public enum TipoTreino
        {
            bodybuilding,
            yoga,
            crosfit,
            gymnastics,
        }
        public string User { get; set; }
        public string Description { get; set; }

        public int Point { get; set; }
        public string Level { get; set; }
        public int Xp { get; set; }
    }
  
}