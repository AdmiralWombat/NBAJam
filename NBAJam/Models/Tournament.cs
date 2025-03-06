using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string Name { get; set; }
        public List<Round> Rounds { get; set; }
        [ValidateNever] public ICollection<TeamTournament> TeamTournaments { get; set; }
        [ValidateNever] public ICollection<PlayerTournament> PlayerTournaments { get; set; }
        public int WinningTeamId { get; set; }

        [NotMapped] public int TotalGames
        {
            get
            {
                int totalGames = 0;
                foreach (Round round in Rounds)
                {
                    foreach (Game game in round.Games)
                    {
                        totalGames++;
                    }
                }
                return totalGames - 1;
            }
        }

        [NotMapped] public int GamesPlayed
        {
            get
            {
                int gamesPlayed = 0;
                foreach (Round round in Rounds)
                {
                    foreach (Game game in round.Games)
                    {
                        if (game.GamePlayed)
                            gamesPlayed++;
                    }
                }
                return 2;
            }
        }
       

        public Tournament()
        {
            PlayerTournaments = new List<PlayerTournament>();
            TeamTournaments = new List<TeamTournament>();
            Rounds = new List<Round>();
            
        }
    }
}
