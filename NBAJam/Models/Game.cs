using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NBAJam.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public List<Team> Teams { get; set; }
        public List<int> TeamPoints { get; set; }

        public Game()
        {
            Teams = new List<Team>();
            TeamPoints = new List<int>();
        }
   

    }
}
