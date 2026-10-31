using Microsoft.AspNetCore.Mvc;

namespace CanteenX.API.Controllers
{
    public class CanteenOwnersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
