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
            var teams = await _teams.GetAllAsync();
            return View(await _teams.GetAllAsync());
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
