using Microsoft.AspNetCore.Mvc;
using NBAJam.Models;
using NBAJam.Data;

namespace NBAJam.Controllers
{
    public class TeamController : Controller
    {
        private Repository<Team> _teams;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public TeamController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _teams = new Repository<Team>(context);

            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var teams = await _teams.GetAllAsync(new QueryOptions<Team>
            {
                Includes = "Players, TeamTournaments.Tournament"
            });

            if (teams != null)
            {
                foreach (var team in teams)
                {
                    team.TournamentsWon = 0;
                    foreach (var teamTournament in team.TeamTournaments)
                    {
                        if (teamTournament.Tournament.WinningTeamId == team.TeamId)
                            team.TournamentsWon++;
                    }
                }
            }

            return View(teams);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Team team)
        {
            await _teams.DeleteAsync(team.TeamId);
            return RedirectToAction("Index");
        }
    }
}
