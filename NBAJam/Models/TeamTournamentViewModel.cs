namespace NBAJam.Models
{
    public class TeamTournamentViewModel
    {
        public List<Team> Teams { get; set; }
        public List<int> TeamIds { get; set; }

        public List<Player> Players { get; set; }
        public List<int> PlayerIds { get; set; }

        public string Name { get; set; }

        public int TournamentId { get; set; }

        public bool RandomSetup { get; set; }

        public TeamTournamentViewModel()
        {
            Teams = new List<Team>();
            TeamIds = new List<int>();
            Players = new List<Player>();
            PlayerIds = new List<int>();
        }
    }
}
