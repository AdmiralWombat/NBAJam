using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NBAJam.Models
{
    public class Team
    {
        public int TeamId { get; set; }
        [ValidateNever] public List<PlayerTeam> PlayerTeams { get; set; }
        public int TournamentsWon { get; set; }

        public bool ByeTeam { get; set; }

        [ValidateNever] public ICollection<TeamTournament> TeamTournaments { get; set; }

        [NotMapped] public string Name
        {
            get
            {
                string name = "";
                if (ByeTeam)
                    name = "Bye";
                else
                {
                    int playerCount = 0;
                    foreach (var playerTeam in PlayerTeams)
                    {
                        name += playerTeam.Player.Name;
                        playerCount++;
                        if (playerCount < PlayerTeams.Count)
                            name += " and ";
                    }
                }
                return name;
            }
        }

        public Team()
        {
            PlayerTeams = new List<PlayerTeam>();
            TeamTournaments = new List<TeamTournament>();
        }
    }
}
