using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Player
    {
        [Key]
        public int PlayerId { get; set; }
        public string Name { get; set; }

        public int TournamentsWon { get; set; }

        [ValidateNever] public ICollection<PlayerTournament> PlayerTournaments { get; set; }
    }
}
