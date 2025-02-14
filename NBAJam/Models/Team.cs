using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Team
    {
        public int TeamId { get; set; }
        public List<Player> Players { get; set; }
        public int TournamentsWon { get; set; }

        public bool ByeTeam { get; set; }

        [ValidateNever] public ICollection<TeamTournament> TeamTournaments { get; set; }

        public Team()
        {
            Players = new List<Player>();
            TeamTournaments = new List<TeamTournament>();
        }
    }
}
