using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string Name { get; set; }
        public ICollection<Game> Games { get; set; }
        [ValidateNever] public ICollection<TeamTournament> TeamTournaments { get; set; }
        [ValidateNever] public ICollection<PlayerTournament> PlayerTournaments { get; set; }

        public Tournament()
        {
            PlayerTournaments = new List<PlayerTournament>();
            TeamTournaments = new List<TeamTournament>();
            Games = new List<Game>();
        }
    }
}
