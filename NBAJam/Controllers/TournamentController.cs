using Microsoft.AspNetCore.Mvc;
using NBAJam.Models;
using NBAJam.Data;

using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices.Marshalling;

namespace NBAJam.Controllers
{
    public class TournamentController : Controller
    {
        private Repository<Tournament> _tournaments;
        private Repository<Player> _players;
        private Repository<Team> _teams;
        private Repository<Game> _games;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public TournamentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _tournaments = new Repository<Tournament>(context);
            _players = new Repository<Player>(context);
            _teams = new Repository<Team>(context);
            _games = new Repository<Game>(context);


            _webHostEnvironment = webHostEnvironment;

        }

        public async Task<IActionResult> Index()
        {
            return View(await _tournaments.GetAllAsync());
        }

        [HttpGet]
        public async Task<IActionResult> TournamentView(int id)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, Rounds.Games",
            });

            ViewBag.Rounds = (int)Math.Floor(Math.Log2(tournament.PlayerTournaments.Count));

            ViewBag.Games = new int[ViewBag.Rounds];

            int gameCount = 1;
            for (int i = ViewBag.Rounds - 1; i >= 0; i--)
            {
                ViewBag.Games[i] = gameCount;
                gameCount *= 2;
            }

            return View(tournament);
        }

        [HttpGet]
        public async Task<IActionResult> BracketSetup(int id)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, Rounds.Games",
            });

            if (tournament != null)
            {
                ViewBag.NumberOfTeams = tournament.PlayerTournaments.Count / 2;

                ViewBag.Rounds = (int)Math.Floor(Math.Log2(tournament.PlayerTournaments.Count));
                ViewBag.Games = new int[ViewBag.Rounds];

                int gameCount = 1;
                for (int i = ViewBag.Rounds - 1; i >= 0; i--)
                {
                    ViewBag.Games[i] = gameCount;
                    gameCount *= 2;
                }
                return View(tournament);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> BracketSetup(int tournamentId, int[][][] teamIds)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(tournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, Rounds.Games",
            });

            if (tournament != null)
            {
                int round1Games = (int)Math.Floor(Math.Log2(tournament.PlayerTournaments.Count));

                tournament.Rounds.Clear();
                tournament.Rounds.Add(new Round() { RoundNumber = 0 });
                tournament.Rounds[0].Games.Clear();

                for (int i = 0; i < round1Games; i++)
                {
                    Team team1 = await _teams.GetByIdAsync(teamIds[0][i][0], new QueryOptions<Team>
                    {
                        Includes = "Players",
                    });

                    Team team2 = await _teams.GetByIdAsync(teamIds[0][i][1], new QueryOptions<Team>
                    {
                        Includes = "Players",
                    });

                    List<Team> teams = new List<Team>();
                    teams.Add(team1);
                    teams.Add(team2);

                    Game newGame = new Game();
                    //newGame.teams = teams;
                    newGame.Team1 = team1;
                    newGame.Team2 = team2;
                    newGame.TournamentId = tournamentId;
                    newGame.Tournament = tournament;

                    await _games.AddAsync(newGame);

                    tournament.Rounds[0].Games.Add(newGame);
                }
                await _tournaments.UpdateAsync(tournament);

                return RedirectToAction("TournamentView", "Tournament", new { id = tournament.TournamentId });
            }

            return RedirectToAction("Index", "Tournament");
        }


        [HttpGet]
        public async Task<IActionResult> TeamSetup(int id)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team"
            });

            ViewBag.NumberOfTeams = tournament.PlayerTournaments.Count / 2;

            return View(tournament);
        }

        [HttpPost]
        public async Task<IActionResult> TeamSetup(int tournamentId, int[][] playerIds)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(tournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team",
            });

            if (tournament != null)
            {
                for (int i = 0; i < tournament.PlayerTournaments.Count / 2; i++)
                {
                    if (playerIds[i][0] == 0 || playerIds[i][1] == 0) continue;

                    Player player1 = await _players.GetByIdAsync(playerIds[i][0], new QueryOptions<Player> { Includes = "PlayerTournaments.Tournament" });
                    Player player2 = await _players.GetByIdAsync(playerIds[i][1], new QueryOptions<Player> { Includes = "PlayerTournaments.Tournament" });

                    if (player1 == null || player2 == null) continue;

                    if (player1.PlayerId == 0 || player2.PlayerId == 0) continue;

                    var player1Id = player1.PlayerId;
                    var player2Id = player2.PlayerId;

                    /*bool teamExists = await _teams.AnyAsync(t =>
                        t.PlayerIds.ToList().Contains(player1.PlayerId) && t.PlayerIds.ToList().Contains(player2.PlayerId) &&
                        t.PlayerIds.Count == 2);*/

                    var allTeams = await _teams.GetAllAsync();
                    var existingTeam = allTeams.FirstOrDefault(t =>
                        t.Players.Contains(player1) && t.Players.Contains(player2) &&
                        t.Players.Count == 2);


                    if (existingTeam != null)
                    {
                        bool teamExistsInTournament = false;
                        foreach (TeamTournament tt in tournament.TeamTournaments)
                        {
                            if (tt.TeamId == existingTeam.TeamId)
                            {
                                teamExistsInTournament = true;
                                break;
                            }
                        }
                        if (!teamExistsInTournament)
                            tournament.TeamTournaments.Add(new TeamTournament() { TeamId = existingTeam.TeamId, TournamentId = tournament.TournamentId });

                        await _tournaments.UpdateAsync(tournament);

                    }
                    else
                    {
                        Team newTeam = new Team();
                        newTeam.Players.Add(player1);
                        newTeam.Players.Add(player2);

                        await _teams.AddAsync(newTeam);

                        tournament.TeamTournaments.Add(new TeamTournament() { TeamId = newTeam.TeamId, TournamentId = tournament.TournamentId });

                        await _tournaments.UpdateAsync(tournament);
                    }
                }

                //add bye team
                bool buyTeamExistsInTournament = false;
                foreach (TeamTournament tt in tournament.TeamTournaments)
                {
                    if (tt.TeamId == 1)
                    {
                        buyTeamExistsInTournament = true;
                        break;
                    }
                }
                if (!buyTeamExistsInTournament)
                {
                    tournament.TeamTournaments.Add(new TeamTournament() { TeamId = 1, TournamentId = tournament.TournamentId });
                    await _tournaments.UpdateAsync(tournament);
                }

                return RedirectToAction("BracketSetup", "Tournament", new { id = tournament.TournamentId });
            }
            return RedirectToAction("Index", "Tournament");
        }

        [HttpGet]
        public async Task<IActionResult> AddEdit(int id)
        {
            ViewBag.AllPlayers = await _players.GetAllAsync();

            if (id == 0)
            {
                //add
                ViewBag.Operation = "Add";
                return View(new Tournament());
            }
            else
            {
                Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
                {
                    Includes = "PlayerTournaments.Player",
                });
                ViewBag.Operation = "Edit";
                return View(tournament);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(Tournament tournament, int[] playerIDs)
        {
            ViewBag.AllPlayers = await _players.GetAllAsync();
            ViewBag.TournamentPlayers = await _players.GetAllAsync();

            if (ModelState.IsValid)
            {
                if (tournament.TournamentId == 0)
                {
                    foreach (int id in playerIDs)
                    {
                        tournament.PlayerTournaments?.Add(new PlayerTournament { PlayerID = id, TournamentId = tournament.TournamentId });
                    }
                    await _tournaments.AddAsync(tournament);

                }
                else
                {
                    var existingTournament = await _tournaments.GetByIdAsync(tournament.TournamentId, new QueryOptions<Tournament> { Includes = "PlayerTournaments" });

                    if (existingTournament == null)
                    {
                        ModelState.AddModelError("", "Tournament not found.");

                        return View(tournament);
                    }

                    existingTournament.Name = tournament.Name;
                    existingTournament.PlayerTournaments?.Clear();
                    foreach (int id in playerIDs)
                    {
                        existingTournament.PlayerTournaments?.Add(new PlayerTournament { PlayerID = id, TournamentId = tournament.TournamentId });
                    }

                    try
                    {
                        await _tournaments.UpdateAsync(existingTournament);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error: {ex.GetBaseException().Message}");
                        return View(tournament);
                    }
                }
            }
            return RedirectToAction("TeamSetup", "Tournament", new { id = tournament.TournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePlayer(int playerId)
        {
            Console.WriteLine("HERE");

            if (/* removal successful */ true)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPlayer(int playerId)
        {
            if (/* removal successful */ true) // Replace with your actual success condition
            {
                return Ok(); // Return a 200 OK status code
            }
            else
            {
                return BadRequest();
            }
        }

      
        public async Task<IActionResult> test()
        {
            return View();
        }

    }
}


/*
 * 
 * 
*/