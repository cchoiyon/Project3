using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Project3.Shared.Models.ViewModels;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

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
            // Log all claims for debugging
            _logger.LogInformation("User claims:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            
            // Check roles using IsInRole
            bool isRestaurantRep = User.IsInRole("RestaurantRep");
            bool isReviewer = User.IsInRole("Reviewer");
            
            _logger.LogInformation($"User is in RestaurantRep role: {isRestaurantRep}");
            _logger.LogInformation($"User is in Reviewer role: {isReviewer}");
            
            // Redirect based on role
            if (isRestaurantRep)
            {
                _logger.LogInformation("Redirecting to RestaurantRepHome/Index");
                return RedirectToAction("Index", "RestaurantRepHome");
            }
            else if (isReviewer)
            {
                _logger.LogInformation("Redirecting to ReviewerHome/Index");
                return RedirectToAction("Index", "ReviewerHome");
            }
            else
            {
                _logger.LogWarning("User is not in any recognized role, redirecting to Account/Login");
                return RedirectToAction("Login", "Account");
            }
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
