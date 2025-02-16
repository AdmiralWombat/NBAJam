namespace NBAJam.Models
{
    public class Round
    {
        public int RoundId { get; set; }
        public int RoundNumber { get; set; }
        public List<Game> Games { get; set; }

        public Round()
        {
            Games = new List<Game>();   
        }

    }
}
