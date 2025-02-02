using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NBAJam.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public ICollection<Team> Teams { get; set; }
        public ICollection<int> TeamPoints { get; set; }
   

    }
}
