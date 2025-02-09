using Microsoft.AspNetCore.Mvc;
using NBAJam.Data;
using NBAJam.Models;

namespace NBAJam.ViewComponents
{
    public class CreatePlayerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(bool returnToPlayerPage)
        {
            Player player = new Player();
            PlayerViewModel playerViewModel = new PlayerViewModel();
            playerViewModel.ReturnToPlayerPage = returnToPlayerPage;
            return View(playerViewModel);

          
        }

    }
}
