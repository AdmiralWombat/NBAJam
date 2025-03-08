using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NBAJam.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        //public List<Team> Teams { get; set; }
        public Team? Team1 { get; set; }
        public Team? Team2 { get; set; }

        public int Team1Points { get; set; }
        public int Team2Points { get; set; }

        public bool Team1Won { get; set; }
        public bool Team2Won { get; set; }

        [NotMapped] public bool GamePlayed
        {
            get
            {
                return Team1Won || Team2Won || (Team1 != null && Team1.ByeTeam) || (Team2 != null && Team2.ByeTeam);
            }
        }

        public Game()
        {
            
        }
   

    }
}
