using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Player
    {       
        public int PlayerId { get; set; }
        public string Name { get; set; }

        [ValidateNever] public List<PlayerTeam> PlayerTeams { get; set; }

        public int TournamentsWon { get; set; }

        [ValidateNever] public ICollection<PlayerTournament> PlayerTournaments { get; set; }

        public Player()
        {
            PlayerTeams = new List<PlayerTeam>();   
        }
    }
}
