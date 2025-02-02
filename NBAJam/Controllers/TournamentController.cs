using Microsoft.AspNetCore.Mvc;
using NBAJam.Models;
using NBAJam.Data;

namespace NBAJam.Controllers
{
    public class TournamentController : Controller
    {
        private Repository<Tournament> _tournaments;
        private Repository<Player> _players;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public TournamentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _tournaments = new Repository<Tournament>(context);
            _players = new Repository<Player>(context);

            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _tournaments.GetAllAsync());
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
                    Includes = "",
                });
                ViewBag.Operation = "Edit";
                return View(tournament);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(Tournament tournament, int playerIDs)
        {
            return View(tournament);
        }
    }
}
    