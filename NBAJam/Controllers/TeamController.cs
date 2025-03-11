using Microsoft.AspNetCore.Mvc;
using NBAJam.Models;
using NBAJam.Data;

namespace NBAJam.Controllers
{
    public class TeamController : Controller
    {
        private Repository<Team> _teams;
        private Repository<Game> _games;
        private Repository<Tournament> _tournaments;
        private Repository<Round> _rounds;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public TeamController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _teams = new Repository<Team>(context);
            _games = new Repository<Game>(context);
            _tournaments = new Repository<Tournament>(context);
            _rounds = new Repository<Round>(context);

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
        public async Task<IActionResult> Delete(int id)
        {
            IEnumerable<Round> rounds = await _rounds.GetAllAsync();
            foreach (Round round in rounds)
            {
                await _rounds.DeleteAsync(round.RoundId);
                await _rounds.UpdateAsync(round);

            }

            IEnumerable<Game> games = await _games.GetAllAsync();
            foreach (Game game in games)
            {
                if (game.Team1?.TeamId == id)
                {
                    game.Team1 = null;
                    await _games.UpdateAsync(game);
                }
                if (game.Team2?.TeamId == id)
                {
                    game.Team2 = null;
                    await _games.UpdateAsync(game);
                }
            }

            IEnumerable<Tournament> tournaments = await _tournaments.GetAllAsync();
            foreach (Tournament tournament in tournaments)
            {
                foreach (TeamTournament teamTournament in tournament.TeamTournaments)
                {
                    if (teamTournament.TeamId == id)
                    {
                        tournament.TeamTournaments.Remove(teamTournament);
                        await _tournaments.UpdateAsync(tournament);
                        break;
                    }    
                }
            }

            Team team = await _teams.GetByIdAsync(id, new QueryOptions<Team> { Includes = "TeamTournaments" });
            team.TeamTournaments.Clear();
            await _teams.UpdateAsync(team);

            await _teams.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
