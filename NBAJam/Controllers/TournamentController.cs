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
            IEnumerable<Tournament> tournaments = await _tournaments.GetAllAsync(new QueryOptions<Tournament> { Includes = "Rounds" });            

            ViewBag.TeamNames = new Dictionary<int, string>();

            foreach (Tournament tournament in tournaments)
            {
                Team winningTeam = await _teams.GetByIdAsync(tournament.WinningTeamId, new QueryOptions<Team>{ Includes = "PlayerTeams, PlayerTeams.Player" });
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
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, TeamTournaments.Team.PlayerTeams, TeamTournaments.Team.PlayerTeams.Player, Rounds",
            });           

            ViewBag.Rounds = (int)Math.Ceiling(Math.Log2(tournament.PlayerTournaments.Count / 2.0));

            bool updatedTournament = false;

            for (int i = 1; i < ViewBag.Rounds; i++)
            {
                if (tournament.Rounds.ElementAtOrDefault(i) == null)
                {
                    tournament.Rounds.Add(new Round() { RoundNumber = i });
                    updatedTournament = true;
                }
            }

            Team winningTeam = await _teams.GetByIdAsync(tournament.WinningTeamId, new QueryOptions<Team>
            {
                Includes = "PlayerTeams, PlayerTeams.Player",
            });
            ViewBag.WinningTeamName = winningTeam?.Name;


            ViewBag.Games = new int[ViewBag.Rounds];

            int gameCount = 1;
            for (int i = ViewBag.Rounds - 1; i >= 0; i--)
            {
                ViewBag.Games[i] = gameCount;
                for (int j = 0; j < gameCount; j++)
                {  
                    if (j >= tournament.Rounds[i].GameIds.Count || (j < tournament.Rounds[i].GameIds.Count && tournament.Rounds[i].GameIds[j] == 0))
                    {
                        Game newGame = new Game() { TournamentId = id, Tournament = tournament };
                        await _games.AddAsync(newGame);
                        if (newGame != null && newGame.GameId != 0)
                        {
                            tournament.Rounds[i].GameIds.Add(newGame.GameId);
                            updatedTournament = true;
                        }
                    }
                }

                gameCount *= 2;
            }

            //check byes and games Played
            int roundIndex = 0;
            int gamesPlayed = 0;
            foreach (Round round in tournament.Rounds)
            {
                int gameIndex = 0;
                foreach (int gameId in round.GameIds)
                {
                    Game game = await _games.GetByIdAsync(gameId, new QueryOptions<Game> { Includes = "Team1, Team2" });

                    int newGameIndex = gameIndex / 2;
                    int newGameId = tournament.Rounds.ElementAtOrDefault(roundIndex+1)?.GameIds.ElementAtOrDefault(newGameIndex) ?? 0;

                    Game newGame = await _games.GetByIdAsync(newGameId, new QueryOptions<Game> { Includes = "Team1, Team2" });
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
                        game.Team2Won = true;
                        game.Team1Won = false;
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
                        game.Team1Won = true;
                        game.Team2Won = false;
                    }

                    if (game.GamePlayed)
                        gamesPlayed++;

                    gameIndex++;
                }
                roundIndex++;
            }

            if (gamesPlayed != tournament.GamesPlayed && gamesPlayed <= tournament.TotalGames && gamesPlayed >= 0)
            {
                tournament.GamesPlayed = gamesPlayed;
                updatedTournament = true;
            }


            if (updatedTournament)
                await _tournaments.UpdateAsync(tournament);


            // Fetch games for each round
            Dictionary<int, List<Game>> roundGames = new Dictionary<int, List<Game>>();
            foreach (Round round in tournament.Rounds)
            {
                List<Game> games = new List<Game>();
                foreach (int gameId in round.GameIds)
                {
                    Game game = await _games.GetByIdAsync(gameId, new QueryOptions<Game> { Includes = "Team1, Team2" });
                    if (game != null)
                    {
                        games.Add(game);
                    }
                }
                roundGames[round.RoundNumber] = games;
            }
            ViewBag.RoundGames = roundGames;

            return View(tournament);
        }

        [HttpGet]
        public async Task<IActionResult> BracketSetup(TeamTournamentViewModel teamTournamentViewModel)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(teamTournamentViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds, TeamTournaments.Team.PlayerTeams",
            });            

            

            
            ViewBag.Rounds = (int)Math.Ceiling(Math.Log2(teamTournamentViewModel.PlayerIds.Count / 2.0));
            ViewBag.Games = new int[ViewBag.Rounds];

            int gameCount = 1;
            for (int i = ViewBag.Rounds - 1; i >= 0; i--)
            {
                ViewBag.Games[i] = gameCount;
                gameCount *= 2;
            }
            List<Team> teams = new List<Team>();
            if (tournament != null)
            {
                Team byeTeam = await _teams.GetByIdAsync(1, new QueryOptions<Team> { Includes = "PlayerTeams, PlayerTeams.Player" });
                foreach (int gameId in tournament.Rounds[0].GameIds)
                {
                    Game game = await _games.GetByIdAsync(gameId, new QueryOptions<Game> { Includes = "Team1, Team2" });
                    if (game != null && game.Team1 != null)
                        teams.Add(game.Team1);
                    if (game != null && game.Team2 != null)
                        teams.Add(game.Team2);
                }
             
                teams.Add(byeTeam);
            }
            else
            {
                int teamCount = 0;
                foreach (int teamId in teamTournamentViewModel.TeamIds)
                {
                    if (teamId != 1)
                        teamCount++;

                    Team team = await _teams.GetByIdAsync(teamId, new QueryOptions<Team> { Includes = "PlayerTeams, PlayerTeams.Player" });
                    teams.Add(team);
                }


                if (teamTournamentViewModel.RandomSetup)
                {
                    teams = teams.OrderBy(_ => _rng.Next()).ToList();
                    List<int> bracketTeamIds = new List<int>(new int[ViewBag.Games[0] * 2]);
                    //int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(teams.Count - 1)));
                    //int numberOfByes = nextPowerOfTwo - teams.Count - 1;
                    int numberOfByes = (ViewBag.Games[0] * 2) - teamCount;

                    for (int i = 0; i < numberOfByes; i += 2)
                    {
                        bracketTeamIds[(i + 1)] = 1;
                        if (i + 1 < numberOfByes)
                            bracketTeamIds[bracketTeamIds.Count - 1 - i] = 1;
                    }

                    int teamIndex = 0;
                    for (int i = 0; i < teams.Count; i++)
                    {
                        if (!teams[i].ByeTeam)
                        {
                            if (bracketTeamIds[teamIndex] != 1)
                            {
                                bracketTeamIds[teamIndex] = teams[i].TeamId;

                            }
                            else
                                i--;
                            teamIndex++;
                        }
                    }

                    teams.Clear();
                    teamTournamentViewModel.TeamIds.Clear();
                    foreach (int teamId in bracketTeamIds)
                    {
                        teamTournamentViewModel.TeamIds.Add(teamId);
                        teams.Add(await _teams.GetByIdAsync(teamId, new QueryOptions<Team> { Includes = "PlayerTeams, PlayerTeams.Player" }));
                    }
                    

                }
                else
                    teams = teams.OrderBy(i => i.ByeTeam ? 1 : 0).ToList();
            }
          
            return View(new BracketSetupViewModel() { TournamentName = teamTournamentViewModel.Name, Teams = teams, PlayerIds = teamTournamentViewModel.PlayerIds, TeamIds = teamTournamentViewModel.TeamIds });
            
           
        }

        [HttpPost]
        public async Task<IActionResult> BracketSetup(BracketSetupViewModel bracketSetupViewModel, int[][] teamBracketPositionIds)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(bracketSetupViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds",
            });

            if (tournament == null)
            {
                tournament = new Tournament() { Name = bracketSetupViewModel.TournamentName };
                await _tournaments.AddAsync(tournament);

                List<PlayerTournament> playerTournaments = new List<PlayerTournament>();
                List<TeamTournament> teamTournaments = new List<TeamTournament>();

                foreach (int playerId in bracketSetupViewModel.PlayerIds)
                {
                    Player player = await _players.GetByIdAsync(playerId, new QueryOptions<Player> { });
                    if (player != null)
                    {
                        PlayerTournament playerTournament = new PlayerTournament() { Player = player, PlayerID = playerId, Tournament = tournament, TournamentId = tournament.TournamentId };
                        playerTournaments.Add(playerTournament);
                    }
                }

                foreach (int teamId in bracketSetupViewModel.TeamIds)
                {
                    Team team = await _teams.GetByIdAsync(teamId, new QueryOptions<Team> { });
                    if (team != null && !team.ByeTeam)
                    {
                        TeamTournament teamTournament = new TeamTournament() { Team = team, TeamId = teamId, Tournament = tournament, TournamentId = tournament.TournamentId };
                        teamTournaments.Add(teamTournament);
                    }
                }

                Team byeTeam = await _teams.GetByIdAsync(1, new QueryOptions<Team> { });
                AddByeTeam(ref tournament, byeTeam);
               
                tournament.WinningTeamId = 0;
                tournament.PlayerTournaments = playerTournaments;
                tournament.TeamTournaments = teamTournaments;


                await _tournaments.UpdateAsync(tournament);
            }


            if (tournament != null)
            {
                int round1Games = teamBracketPositionIds.Count();

                tournament.Rounds.Clear();
                tournament.Rounds.Add(new Round() { RoundNumber = 0 });
                tournament.Rounds[0].GameIds.Clear();

                for (int i = 0; i < round1Games; i++)
                {
                    Team team1 = await _teams.GetByIdAsync(teamBracketPositionIds[i][0], new QueryOptions<Team>
                    {
                        Includes = "PlayerTeams, PlayerTeams.Player",
                    });

                    Team team2 = await _teams.GetByIdAsync(teamBracketPositionIds[i][1], new QueryOptions<Team>
                    {
                        Includes = "PlayerTeams, PlayerTeams.Player",
                    });

                    

                    Game newGame = new Game();
                    //newGame.teams = teams;
                    newGame.Team1 = team1;
                    newGame.Team2 = team2;
                    newGame.TournamentId = tournament.TournamentId;
                    newGame.Tournament = tournament;

                    await _games.AddAsync(newGame);
                    
                    tournament.Rounds[0].GameIds.Add(newGame.GameId);
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
                    Team newTeam = await _teams.GetByIdAsync(teamId, new QueryOptions<Team> { Includes = "PlayerTeams, PlayerTeams.Player, TeamTournaments" });
                    if (newTeam != null && !newTeam.ByeTeam)
                    {
                        TeamTournament teamTournament = new TeamTournament() { Team = newTeam, TeamId = teamId, Tournament = tournament, TournamentId = bracketSetupViewModel.TournamentId };
                        tournament.TeamTournaments.Add(teamTournament);
                    }
                }

                Team byeTeam = await _teams.GetByIdAsync(1, new QueryOptions<Team> { });
                AddByeTeam(ref tournament, byeTeam);

                tournament.WinningTeamId = 0;

                tournament.Name = bracketSetupViewModel.TournamentName;


                await _tournaments.UpdateAsync(tournament);



                Tournament newTournament = await _tournaments.GetByIdAsync(tournament.TournamentId, new QueryOptions<Tournament>
                {
                    Includes = "PlayerTournaments.Player, TeamTournaments.Team, Rounds",
                });

                return RedirectToAction("TournamentView", "Tournament", new { id = tournament.TournamentId });
            }

            return RedirectToAction("Index", "Tournament");
        }


        [HttpGet]
        public async Task<IActionResult> TeamSetup(PlayerTournamentViewModel playerTournamentViewModel)
        {
            Tournament tournament = await _tournaments.GetByIdAsync(playerTournamentViewModel.TournamentId, new QueryOptions<Tournament>
            {
                Includes = "PlayerTournaments.Player, TeamTournaments.Team, TeamTournaments.Team.PlayerTeams"
            });

            int tournamentId = 0;

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

            List<Team> teamsNoByes = new List<Team>();
            List<int> teamIdsNoByes = new List<int>();
            if (tournament == null && playerTournamentViewModel != null)
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
                           // await _teams.AddAsync(existingTeam);

                            PlayerTeam newPlayerTeam1 = new PlayerTeam() { Player = player1, PlayerId = player1.PlayerId, Team = existingTeam, TeamId = existingTeam.TeamId };
                            PlayerTeam newPlayerTeam2 = new PlayerTeam() { Player = player2, PlayerId = player2.PlayerId, Team = existingTeam, TeamId = existingTeam.TeamId };

                            existingTeam.PlayerTeams.Add(newPlayerTeam1);
                            existingTeam.PlayerTeams.Add(newPlayerTeam2);

                           // await _teams.UpdateAsync(existingTeam);
                        }

                        //TeamTournament teamTournament = new TeamTournament() { Team = existingTeam, TeamId = existingTeam.TeamId, Tournament = tournament, TournamentId = playerTournamentViewModel.TournamentId };
                       // tournament.TeamTournaments.Add(teamTournament);

                        teams.Add(existingTeam);
                        teamIds.Add(existingTeam.TeamId);


                        randomPlayers.RemoveAt(randomPlayers.Count - 1);
                        randomPlayers.RemoveAt(randomPlayers.Count - 1);
                    }
                }
                
            }
            else
            {
                tournamentId = tournament.TournamentId;
                foreach (TeamTournament teamTournament in tournament.TeamTournaments)
                {
                    teams.Add(teamTournament.Team);
                    teamIds.Add(teamTournament.TeamId);
                }
            }

            teamsNoByes = teams.Where(x => x.TeamId != 1).ToList();

            return View(new TeamTournamentViewModel() { Name = playerTournamentViewModel.Name, Players = players, Teams = teams, TournamentId = tournamentId, TeamIds = teamIds, TeamIdsNoByes = teamIdsNoByes, TeamsNoByes = teamsNoByes, PlayerIds = playerIds, RandomSetup = playerTournamentViewModel.RandomSetup });

        }

        [HttpPost]
        public async Task<IActionResult> TeamSetup(TeamTournamentViewModel teamTournamentViewModel, int[][] playerTeamPositionIds)
        {
            //Tournament tournament = await _tournaments.GetByIdAsync(teamTournamentViewModel.TournamentId, new QueryOptions<Tournament>
            //{
             //   Includes = "PlayerTournaments.Player, TeamTournaments.Team",
            //});

            teamTournamentViewModel.PlayerIds.Clear();

            teamTournamentViewModel.TeamIds.RemoveAll(i => i == 0);

            if (teamTournamentViewModel != null)
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

                    var allTeams = await _teams.GetAllAsync(new QueryOptions<Team> { Includes = "PlayerTeams, PlayerTeams.Player" });

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

                        existingTeam.PlayerTeams.Add(newPlayerTeam1);
                        existingTeam.PlayerTeams.Add(newPlayerTeam2);
                        
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
        public async Task<IActionResult> PlayerSetup(int id)
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
        public async Task<IActionResult> PlayerSetup(PlayerTournamentViewModel playerTournamentViewModel, int[] playerIDs)
        {
            ViewBag.AllPlayers = await _players.GetAllAsync();
            ViewBag.TournamentPlayers = await _players.GetAllAsync();

            if (ModelState.IsValid)
            {          
                if (playerTournamentViewModel.TournamentId == 0)
                {
                    //Tournament newTournament = new Tournament() { Name = playerTournamentViewModel.Name };
                   // await _tournaments.AddAsync(newTournament);

                    //playerTournamentViewModel.TournamentId = newTournament.TournamentId;
                }
                else
                {
                    playerTournamentViewModel.RandomTeams = false;
                    playerTournamentViewModel.RandomSetup = false;
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
                Includes = "Rounds"
            });

            if (tournament != null)
            {               
                foreach (Round round in tournament.Rounds.ToList())
                {
                    foreach (int gameId in round.GameIds)
                    {
                        await _games.DeleteAsync(gameId);
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
                Includes = "Team1, Team2, Tournament, Team1.PlayerTeams.Player, Team2.PlayerTeams.Player"
            });

            Team team1 = await _teams.GetByIdAsync(team1Id, new QueryOptions<Team> { 
                Includes = "PlayerTeams.Player"
            });

            Team team2 = await _teams.GetByIdAsync(team2Id, new QueryOptions<Team>
            {
                Includes = "PlayerTeams.Player"
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
                Includes = "Rounds"
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
                    int newGameId = newRound.GameIds.ElementAtOrDefault(newGameIndex);
                    if (newGameId != 0)
                    {
                        Game newGame = await _games.GetByIdAsync(newGameId, new QueryOptions<Game> { Includes = "Team1, Team2, Tournament" });
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
            }
            
          

            await _tournaments.UpdateAsync(tournament);


            return RedirectToAction("TournamentView", "Tournament", new {id = tournamentId});
        }

        private void AddByeTeam(ref Tournament tournament, Team byeTeam)
        {
            if (byeTeam != null && tournament != null)
            {
                TeamTournament byeTeamTournament = new TeamTournament() { Team = byeTeam, TeamId = byeTeam.TeamId, Tournament = tournament, TournamentId = tournament.TournamentId };

                if (tournament != null && tournament.TeamTournaments != null)
                {
                    bool containsByeTeam = false;
                    foreach (TeamTournament teamTournament in tournament.TeamTournaments)
                    {
                        if (teamTournament != null && teamTournament.TeamId == byeTeam.TeamId)
                        {
                            containsByeTeam = true;
                            break;
                        }
                    }
                    if (!containsByeTeam)
                        tournament.TeamTournaments.Add(byeTeamTournament);
                }
            }
        }
        
    }   

}


/*
 * 
 * 
*/