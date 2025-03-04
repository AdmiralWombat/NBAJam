namespace NBAJam.Models
{
    public class BracketSetupViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public List<Team> Teams { get; set; }
        public List<int> TeamIds { get; set; }
        public List<Player> Players { get; set; }
        public List<int> PlayerIds { get; set; }

        public BracketSetupViewModel()
        {
            Teams = new List<Team>();
            TeamIds = new List<int>();

            Players = new List<Player>();
            PlayerIds = new List<int>();
        }

    }
}
