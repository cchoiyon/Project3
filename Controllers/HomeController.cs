using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Project3.Models.ViewModels;

namespace Project3.Controllers // Ensure namespace matches your project
{
    [Authorize] // <<< ADD THIS ATTRIBUTE: Requires users to be logged in to access any action in this controller
    public class HomeController : Controller
    {
        // Logger is optional, you can keep it or remove it if not used
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // This Index action will now only be accessible after login.
        // You might later change this to show a specific dashboard based on user type.
        public IActionResult Index()
        {
            // Example: Check user type from session and redirect
            // string userType = HttpContext.Session.GetString("UserType");
            // if (userType == "reviewer") {
            //     return RedirectToAction("Index", "ReviewerHome"); // Redirect to reviewer dashboard
            // } else if (userType == "restaurantRep") {
            //     return RedirectToAction("Index", "RestaurantRepHome"); // Redirect to rep dashboard
            // } else {
            //     // Unknown user type or session issue, maybe redirect to login?
            //     return RedirectToAction("Login", "Account");
            // }

            // For now, just return the default view if authorized
            return View();
        }

        // The Privacy action is also protected by the [Authorize] attribute on the class
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous] // <<< Optional: Allow anyone (even unauthenticated) to see the error page
        public IActionResult Error()
        {
            // Make sure you have an ErrorViewModel defined in your Models folder
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            return View(errorViewModel);
        }
    }
}
