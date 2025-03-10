namespace NBAJam.Models
{
    public class PlayerTournamentViewModel
    {
        public List<int> PlayersIds { get; set; } 
        public List<Player> Players { get; set; }

        public string Name { get; set; }
        public int TournamentId { get; set; }

        public bool RandomTeams { get; set; }
        public bool RandomSetup { get; set; }

        public PlayerTournamentViewModel() 
        {
            PlayersIds = new List<int>();
            Players = new List<Player>();
        }

    }
}
