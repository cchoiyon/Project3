using Microsoft.AspNetCore.Mvc;

namespace Project3.Controllers
{
    public class RestaurantController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
