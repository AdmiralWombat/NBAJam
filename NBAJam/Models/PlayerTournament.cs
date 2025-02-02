namespace NBAJam.Models
{
    public class PlayerTournament
    {
        public int PlayerID { get; set; }
        public Player Player { get; set; }
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }
    }
}
