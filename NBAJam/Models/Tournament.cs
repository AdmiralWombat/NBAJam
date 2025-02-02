using System.ComponentModel.DataAnnotations;

namespace NBAJam.Models
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string Name { get; set; }
        public ICollection<Game> Games { get; set; }
        public ICollection<TeamTournament> TeamTournaments { get; set; }
        public ICollection<PlayerTournament> PlayerTournaments { get; set; }
    }
}
