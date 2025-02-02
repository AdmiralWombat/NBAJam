namespace NBAJam.Models
{
    public class TeamTournament
    {
        public int TeamId { get; set; }
        public Team Team { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
