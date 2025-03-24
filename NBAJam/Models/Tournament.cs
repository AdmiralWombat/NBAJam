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
                    foreach (int gameId in round.GameIds)
                    {
                        totalGames++;
                    }
                }
                return totalGames;
            }
        }

        public int GamesPlayed { get; set; }
        public Tournament()
        {
            PlayerTournaments = new List<PlayerTournament>();
            TeamTournaments = new List<TeamTournament>();
            Rounds = new List<Round>();
            GamesPlayed = 0;
        }
    }
}
