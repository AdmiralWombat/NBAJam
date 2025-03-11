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
        private Repository<Round> _rounds;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private Random _rng;

        public TournamentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _tournaments = new Repository<Tournament>(context);
            _players = new Repository<Player>(context);
            _teams = new Repository<Team>(context);
            _games = new Repository<Game>(context);
            _rounds = new Repository<Round>(context);

            _rng = new Random();

            _webHostEnvironment = webHostEnvironment;

        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Tournament> tournaments = await _tournaments.GetAllAsync(new QueryOptions<Tournament> { Includes = "Rounds, Rounds.Games, Rounds.Games.Team1, Rounds.Games.Team2, Rounds.Games.Team1.Players, Rounds.Games.Team2.Players" });

            ViewBag.TeamNames = new Dictionary<int, string>();

            foreach (Tournament tournament in tournaments)
            {
                Team winningTeam = await _teams.GetByIdAsync(tournament.WinningTeamId, new QueryOptions<Team>{ Includes = "Players" });
                if (winningTeam != null)
                    ViewBag.TeamNames.TryAdd(tournament.TournamentId, winningTeam.Name);
            }
            return View(tournaments);
        }

        [HttpGet]
        public async Task<IActionResult> TournamentView(int id)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, Rounds.Games",
            });

            ViewBag.Rounds = (int)Math.Ceiling(Math.Log2(tournament.PlayerTournaments.Count / 2.0));

            bool updatedTournament = false;

            for (int i = 1; i < ViewBag.Rounds; i++)
            {
                if (tournament.Rounds.ElementAtOrDefault(i) == null)
                {
                    tournament.Rounds.Add(new Round());
                    updatedTournament = true;
                }
            }

            Team winningTeam = await _teams.GetByIdAsync(tournament.WinningTeamId, new QueryOptions<Team>
            {
                Includes = "Players",
            });
            ViewBag.WinningTeamName = winningTeam?.Name;


            ViewBag.Games = new int[ViewBag.Rounds];

            int gameCount = 1;
            for (int i = ViewBag.Rounds - 1; i >= 0; i--)
            {
                ViewBag.Games[i] = gameCount;
                for (int j = 0; j < gameCount; j++)
                {
                    if (tournament.Rounds[i].Games.ElementAtOrDefault(j) == null)
                    {
                        Game newGame = new Game() { TournamentId = id, Tournament = tournament };
                        await _games.AddAsync(newGame);
                        tournament.Rounds[i].Games.Add(newGame);
                        updatedTournament = true;
                    }
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
        public async Task<IActionResult> BracketSetup(TeamTournamentViewModel teamTournamentViewModel)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(teamTournamentViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, Rounds.Games",
            });            

            if (true)
            {
                ViewBag.Rounds = (int)Math.Ceiling(Math.Log2(teamTournamentViewModel.PlayerIds.Count / 2.0));
                ViewBag.Games = new int[ViewBag.Rounds];

                int gameCount = 1;
                for (int i = ViewBag.Rounds - 1; i >= 0; i--)
                {
                    ViewBag.Games[i] = gameCount;
                    gameCount *= 2;
                }

                List<Team> teams = new List<Team>();
                foreach (int teamId in teamTournamentViewModel.TeamIds)
                {
                    Team team = await _teams.GetByIdAsync(teamId, new QueryOptions<Team> { Includes = "Players" });
                    teams.Add(team);
                }

                return View(new BracketSetupViewModel() { TournamentName = teamTournamentViewModel.Name, Teams = teams, PlayerIds = teamTournamentViewModel.PlayerIds, TeamIds = teamTournamentViewModel.TeamIds});
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> BracketSetup(BracketSetupViewModel bracketSetupViewModel, int[][] teamBracketPositionIds)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(bracketSetupViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, Rounds.Games",
            });

            if (tournament != null)
            {
                int round1Games = (int)Math.Ceiling(Math.Log2(bracketSetupViewModel.PlayerIds.Count / 2.0));

                tournament.Rounds.Clear();
                tournament.Rounds.Add(new Round() { RoundNumber = 0 });
                tournament.Rounds[0].Games.Clear();

                for (int i = 0; i < round1Games; i++)
                {
                    Team team1 = await _teams.GetByIdAsync(teamBracketPositionIds[i][0], new QueryOptions<Team>
                    {
                        Includes = "Players",
                    });

                    Team team2 = await _teams.GetByIdAsync(teamBracketPositionIds[i][1], new QueryOptions<Team>
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
                    newGame.TournamentId = tournament.TournamentId;
                    newGame.Tournament = tournament;

                    await _games.AddAsync(newGame);

                    tournament.Rounds[0].Games.Add(newGame);
                }

                tournament.PlayerTournaments.Clear();
                foreach (int playerId in bracketSetupViewModel.PlayerIds)
                {
                    Player newPlayer = await _players.GetByIdAsync(playerId, new QueryOptions<Player> { });
                    if (newPlayer != null)
                    {
                        PlayerTournament playerTournament = new PlayerTournament() { Player = newPlayer, PlayerID = playerId, Tournament = tournament, TournamentId = bracketSetupViewModel.TournamentId };
                        tournament.PlayerTournaments.Add(playerTournament);
                    }
                }

                tournament.TeamTournaments.Clear();
                foreach (int teamId in bracketSetupViewModel.TeamIds)
                {
                    Team newTeam = await _teams.GetByIdAsync(teamId, new QueryOptions<Team> { Includes = "Players, TeamTournaments" });
                    if (newTeam != null)
                    {
                        TeamTournament teamTournament = new TeamTournament() { Team = newTeam, TeamId = teamId, Tournament = tournament, TournamentId = bracketSetupViewModel.TournamentId };
                        tournament.TeamTournaments.Add(teamTournament);
                    }
                }

                tournament.WinningTeamId = 0;

                tournament.Name = bracketSetupViewModel.TournamentName;


                await _tournaments.UpdateAsync(tournament);

                return RedirectToAction("TournamentView", "Tournament", new { id = tournament.TournamentId });
            }

            return RedirectToAction("Index", "Tournament");
        }


        [HttpGet]
        public async Task<IActionResult> TeamSetup(PlayerTournamentViewModel playerTournamentViewModel)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(playerTournamentViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team"
            });

            ViewBag.NumberOfTeams = playerTournamentViewModel.PlayersIds.Count / 2;
            

            List<Player> players = new List<Player>();
            List<int> playerIds = new List<int>();
            foreach (int playerId in playerTournamentViewModel.PlayersIds)
            {
                Player player = await _players.GetByIdAsync(playerId, new QueryOptions<Player> { });
                if (player != null)
                {
                    players.Add(player);
                    playerIds.Add(playerId);
                }
            }

            List<Team> teams = new List<Team>();
            List<int> teamIds = new List<int>();
            if (tournament != null && tournament.TeamTournaments != null)
            {
                if (playerTournamentViewModel.RandomTeams)
                {
                    var allTeams = await _teams.GetAllAsync();

                    List<Player> randomPlayers = new List<Player>(players);
                    randomPlayers = randomPlayers.OrderBy(_ => _rng.Next()).ToList();
                    for (int i = 0; i < players.Count / 2; i++)
                    {
                        Player player1 = randomPlayers[randomPlayers.Count - 1];
                        Player player2 = randomPlayers[randomPlayers.Count - 2];                        

                        Team? existingTeam = null;

                        foreach (Team team in allTeams)
                        {
                            if (team.PlayerTeams.Count == 2)
                            {
                                if ((team.PlayerTeams[0].PlayerId == player1.PlayerId && team.PlayerTeams[1].PlayerId == player2.PlayerId) ||
                                    (team.PlayerTeams[0].PlayerId == player2.PlayerId && team.PlayerTeams[1].PlayerId == player1.PlayerId))
                                {
                                    existingTeam = team;
                                    break;
                                }
                            }
                        }


                        if (existingTeam == null)
                        {
                            existingTeam = new Team();
                            await _teams.AddAsync(existingTeam);

                            PlayerTeam newPlayerTeam1 = new PlayerTeam() { Player = player1, PlayerId = player1.PlayerId, Team = existingTeam, TeamId = existingTeam.TeamId };
                            PlayerTeam newPlayerTeam2 = new PlayerTeam() { Player = player2, PlayerId = player2.PlayerId, Team = existingTeam, TeamId = existingTeam.TeamId };
                            List<PlayerTeam> newPlayerTeams = new List<PlayerTeam>();
                            newPlayerTeams.Add(newPlayerTeam1);
                            newPlayerTeams.Add(newPlayerTeam2);
                            existingTeam = new Team() { PlayerTeams = newPlayerTeams };
                            await _teams.UpdateAsync(existingTeam);
                        }
                        
                        TeamTournament teamTournament = new TeamTournament() { Team = existingTeam, TeamId = existingTeam.TeamId, Tournament = tournament, TournamentId = playerTournamentViewModel.TournamentId };
                        tournament.TeamTournaments.Add(teamTournament);

                        randomPlayers.RemoveAt(randomPlayers.Count - 1);
                        randomPlayers.RemoveAt(randomPlayers.Count - 1);
                    }
                }

                foreach (TeamTournament teamTournament in tournament.TeamTournaments)
                {
                    teams.Add(teamTournament.Team);
                    teamIds.Add(teamTournament.TeamId);
                }

                return View(new TeamTournamentViewModel() { Name = tournament.Name, Players = players, Teams = teams, TournamentId = tournament.TournamentId, TeamIds = teamIds, PlayerIds = playerIds });
            }


            return RedirectToAction("Index", "Tournament");
        }

        [HttpPost]
        public async Task<IActionResult> TeamSetup(TeamTournamentViewModel teamTournamentViewModel, int[][] playerTeamPositionIds)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(teamTournamentViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team",
            });

            teamTournamentViewModel.PlayerIds.Clear();

            if (tournament != null)
            {
                for (int i = 0; i < playerTeamPositionIds.Length; i++)
                {
                    if (playerTeamPositionIds[i][0] == 0 || playerTeamPositionIds[i][1] == 0) continue;

                    Player player1 = await _players.GetByIdAsync(playerTeamPositionIds[i][0], new QueryOptions<Player> { Includes = "PlayerTournaments.Tournament" });
                    Player player2 = await _players.GetByIdAsync(playerTeamPositionIds[i][1], new QueryOptions<Player> { Includes = "PlayerTournaments.Tournament" });

                    if (player1 == null || player2 == null) continue;

                    if (player1.PlayerId == 0 || player2.PlayerId == 0) continue;

                    var player1Id = player1.PlayerId;
                    var player2Id = player2.PlayerId;
                  
                    teamTournamentViewModel.PlayerIds.Add(player1Id);
                    teamTournamentViewModel.PlayerIds.Add(player2Id);

                    var allTeams = await _teams.GetAllAsync();

                    Team? existingTeam = null;

                    foreach (Team team in allTeams)
                    {
                        if (team.PlayerTeams.Count == 2)
                        {
                            if ((team.PlayerTeams[0].PlayerId == player1.PlayerId && team.PlayerTeams[1].PlayerId == player2.PlayerId) ||
                                (team.PlayerTeams[0].PlayerId == player2.PlayerId && team.PlayerTeams[1].PlayerId == player1.PlayerId))
                            {
                                existingTeam = team;
                                break;
                            }
                        }
                    }
                   

                    if (existingTeam != null)
                    {                        
                        if (!teamTournamentViewModel.TeamIds.Contains(existingTeam.TeamId))
                        {
                            teamTournamentViewModel.TeamIds.Add(existingTeam.TeamId);
                        }
                    }
                    else
                    {
                        existingTeam = new Team();
                        await _teams.AddAsync(existingTeam);

                        PlayerTeam newPlayerTeam1 = new PlayerTeam() { Player = player1, PlayerId = player1.PlayerId, Team = existingTeam, TeamId = existingTeam.TeamId };
                        PlayerTeam newPlayerTeam2 = new PlayerTeam() { Player = player2, PlayerId = player2.PlayerId, Team = existingTeam, TeamId = existingTeam.TeamId };
                        List<PlayerTeam> newPlayerTeams = new List<PlayerTeam>();
                        newPlayerTeams.Add(newPlayerTeam1);
                        newPlayerTeams.Add(newPlayerTeam2);
                        existingTeam = new Team() { PlayerTeams = newPlayerTeams };
                        await _teams.UpdateAsync(existingTeam);

                        teamTournamentViewModel.TeamIds.Add(existingTeam.TeamId);                        
                    }
                }
               
                //add bye team               
                if (!teamTournamentViewModel.TeamIds.Contains(1))
                {
                    teamTournamentViewModel.TeamIds.Add(1);
                }
                return RedirectToAction("BracketSetup", "Tournament", teamTournamentViewModel);
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
                return View(new PlayerTournamentViewModel());
            }
            else
            {
                Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
                {
                    Includes = "PlayerTournaments.Player",
                });
                ViewBag.Operation = "Edit";
                
                
                List<int> playerIds = new List<int>();
                List<Player> players = new List<Player>();
                foreach (PlayerTournament playerTournament in tournament.PlayerTournaments)
                {
                    playerIds.Add(playerTournament.Player.PlayerId);
                    players.Add(playerTournament.Player);
                    
                }

                return View(new PlayerTournamentViewModel() { PlayersIds = playerIds, Players = players, Name = tournament.Name, TournamentId = id});
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(PlayerTournamentViewModel playerTournamentViewModel, int[] playerIDs)
        {
            ViewBag.AllPlayers = await _players.GetAllAsync();
            ViewBag.TournamentPlayers = await _players.GetAllAsync();

            if (ModelState.IsValid)
            {          
                if (playerTournamentViewModel.TournamentId == 0)
                {
                    Tournament newTournament = new Tournament() { Name = playerTournamentViewModel.Name };
                    await _tournaments.AddAsync(newTournament);

                    playerTournamentViewModel.TournamentId = newTournament.TournamentId;
                }
                else
                {
                    playerTournamentViewModel.RandomTeams = false;
                }

                foreach (int playerId in playerIDs)
                {
                    playerTournamentViewModel.PlayersIds.Add(playerId);
                }               

                return RedirectToAction("TeamSetup", "Tournament",  playerTournamentViewModel);
            }

           

            return View(playerTournamentViewModel);
        }

        public async Task<IActionResult> Delete(int id)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(id, new QueryOptions<Tournament>
            {
                Includes = "Rounds, Rounds.Games"
            });

            if (tournament != null)
            {               
                foreach (Round round in tournament.Rounds.ToList())
                {
                    foreach (Game game in round.Games.ToList())
                    {
                        await _games.DeleteAsync(game.GameId);
                    }
                    
                    await _rounds.DeleteAsync(round.RoundId);
                }
                                
                await _tournaments.DeleteAsync(tournament.TournamentId);
            }

            return RedirectToAction("Index", "Tournament");
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
            if (round + 1 == tournament.Rounds.Count)
            {
                if (game.Team1Won)
                {
                    tournament.WinningTeamId = team1Id;
                }
                else if (game.Team2Won)
                {
                    tournament.WinningTeamId = team2Id;
                }
            }
            else
            {
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