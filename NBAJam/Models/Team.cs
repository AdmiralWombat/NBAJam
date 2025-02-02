using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Team
    {
        public int TeamId { get; set; }
        public ICollection<Player> Players { get; set; }
        public int TournamentsWon { get; set; }

        [ValidateNever] public ICollection<TeamTournament> TeamTournaments { get; set; }
    }
}
