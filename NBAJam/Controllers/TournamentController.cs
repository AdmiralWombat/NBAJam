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

            bool updatedTournament = false;

            for (int i = 1; i < ViewBag.Rounds; i++)
            {
                if (tournament.Rounds.ElementAtOrDefault(i) == null)
                {
                    tournament.Rounds.Add(new Round());
                    updatedTournament = true;
                }
            }


            ViewBag.Games = new int[ViewBag.Rounds];

            int gameCount = 1;
            for (int i = ViewBag.Rounds - 1; i >= 0; i--)
            {
                ViewBag.Games[i] = gameCount;
                if (tournament.Rounds[i].Games.ElementAtOrDefault(i) == null)
                {
                    Game newGame = new Game() { TournamentId = id, Tournament = tournament}; 
                    await _games.AddAsync(newGame);
                    tournament.Rounds[i].Games.Add(newGame);
                    updatedTournament = true;
                }

                gameCount *= 2;
            }

            //check byes
            int roundIndex = 0;            
            foreach (Round round in tournament.Rounds)
            {
                int gameIndex = 0;
                foreach (Game game in round.Games)
                {
                    int newGameIndex = gameIndex / 2;
                    Game newGame = tournament.Rounds.ElementAtOrDefault(roundIndex++)?.Games.ElementAtOrDefault(newGameIndex);
                    if (game != null && game.Team1 != null && game.Team1.ByeTeam)
                    {
                        if (newGame != null)
                        {
                            if (gameIndex % 2 == 0 && newGame.Team1 == null)
                            {
                                newGame.Team1 = game.Team2;
                                await _games.UpdateAsync(newGame);
                                updatedTournament = true;
                            }
                            else if (gameIndex % 2 != 0 && newGame.Team2 == null)
                            {
                                newGame.Team2 = game.Team2;
                                await _games.UpdateAsync(newGame);
                                updatedTournament = true;
                            }                           
                        }
                    }
                    else if (game != null && game.Team2 != null && game.Team2.ByeTeam)
                    {
                        if (newGame != null)
                        {
                            if (gameIndex % 2 == 0 && newGame.Team1 == null)
                            {
                                newGame.Team1 = game.Team1;
                                await _games.UpdateAsync(newGame);
                                updatedTournament = true;
                            }
                            else if (gameIndex % 2 != 0 && newGame.Team2 == null)
                            {
                                newGame.Team2 = game.Team1;
                                await _games.UpdateAsync(newGame);
                                updatedTournament = true;
                            }                           
                        }
                    }
                    gameIndex++;
                }
                roundIndex++;
            }


            if (updatedTournament)
                await _tournaments.UpdateAsync(tournament);

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


        [HttpPost]
        public async Task<IActionResult> AddGame(int tournamentId, int gameId, int team1Id, int team2Id, int team1Points, int team2Points, int round, int gameIndex)
        {
            Game game = await _games.GetByIdAsync(gameId, new QueryOptions<Game>
            {
                Includes = "Team1, Team2, Tournament, Team1.Players, Team2.Players"
            });

            Team team1 = await _teams.GetByIdAsync(team1Id, new QueryOptions<Team> { 
                Includes = "Players"
            });

            Team team2 = await _teams.GetByIdAsync(team2Id, new QueryOptions<Team>
            {
                Includes = "Players"
            });

            game.Team1Points = team1Points;
            game.Team2Points = team2Points;
            game.Team1Won = false;
            game.Team2Won = false;

            if (team1Points > team2Points)
            {
                game.Team1Won = true;
            }
            else if (team2Points > team1Points)
            {
                game.Team2Won = true;
            }

            await _games.UpdateAsync(game);

            Tournament tournament = await _tournaments.GetByIdAsync(tournamentId, new QueryOptions<Tournament>
            {
                Includes = "Rounds, Rounds.Games"
            });

            int newGameIndex = gameIndex / 2;
            Round? newRound = tournament.Rounds.ElementAtOrDefault(round + 1);
            if (newRound != null)
            {
                Game? newGame = newRound.Games.ElementAtOrDefault(newGameIndex);
                if (newGame != null)
                {
                    Team winningTeam = team1Points > team2Points ? team1 : team2;                    
                    if (gameIndex % 2 == 0)                    
                        newGame.Team1 = winningTeam;
                    else
                        newGame.Team2 = winningTeam;
                   
                    newGame.Tournament = tournament;
                    newGame.TournamentId = tournamentId;

                    await _games.UpdateAsync(newGame);
                }
            }   
          

            await _tournaments.UpdateAsync(tournament);


            return RedirectToAction("TournamentView", "Tournament", new {id = tournamentId});
        }

    }
}


/*
 * 
 * 
*/