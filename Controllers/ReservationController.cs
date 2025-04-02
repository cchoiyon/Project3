using Microsoft.AspNetCore.Mvc;

namespace Project3.Controllers
{
    public class ReservationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
