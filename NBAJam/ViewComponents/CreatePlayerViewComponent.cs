using Microsoft.AspNetCore.Mvc;
using NBAJam.Data;
using NBAJam.Models;

namespace NBAJam.ViewComponents
{
    public class CreatePlayerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            Player player = new Player();
            return View(player);

          
        }

    }
}
