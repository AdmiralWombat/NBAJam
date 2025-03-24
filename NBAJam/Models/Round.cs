namespace NBAJam.Models
{
    public class Round
    {
        public int RoundId { get; set; }
        public int RoundNumber { get; set; }   
        public List<int> GameIds { get; set; }

        public Round()
        {
            GameIds = new List<int>();
        }

    }
}
