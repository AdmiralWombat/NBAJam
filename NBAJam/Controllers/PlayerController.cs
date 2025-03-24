using Microsoft.AspNetCore.Mvc;
using NBAJam.Models;
using NBAJam.Data;

namespace NBAJam.Controllers
{
    public class PlayerController : Controller
    {
        private Repository<Player> _players;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PlayerController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _players = new Repository<Player>(context);

            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _players.GetAllAsync());
        }

        [HttpGet]
        public async Task<IActionResult> AddEdit(int id)
        {
            ViewBag.AllPlayers = await _players.GetAllAsync();
            if (id == 0)
            {
                //add
                ViewBag.Operation = "Add";
                return View(new Player());
            }
            else
            {
                Player player = await _players.GetByIdAsync(id, new QueryOptions<Player>
                {
                    Includes = "",
                });
                ViewBag.Operation = "Edit";
                return View(player);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PlayerId, Name")] Player player, bool playerPage)
        {
            if (ModelState.IsValid)
            {
                await _players.AddAsync(player);
                if (playerPage)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("PlayerSetup", "Tournament");
            }
            return View(player);
        }

        public async Task<IActionResult> Details(int id)
        {
            return View(await _players.GetByIdAsync(id, new QueryOptions<Player>() { Includes = "PlayerTournaments.Tournament" }));           
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await _players.GetByIdAsync(id, new QueryOptions<Player>() { Includes = "PlayerTournaments.Tournament" }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Player player)
        {
            if (ModelState.IsValid)
            {
                await _players.UpdateAsync(player);
                return RedirectToAction("Index");
            }
            return View(player);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Player player)
        {
            await _players.DeleteAsync(player.PlayerId);
            return RedirectToAction("Index");
        }
        
    }
}
