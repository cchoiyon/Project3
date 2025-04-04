using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Needed for [Authorize]
// Add any other necessary using statements for your ViewModels or services
using Project3.Models.ViewModels; // Assuming ReviewerHomeViewModel is here
using System.Security.Claims; // Needed for getting user ID potentially
// using System.Threading.Tasks; // If using async methods
// using System.Linq; // If using LINQ for data retrieval

namespace Project3.Controllers
{
    // Ensure the controller requires authorization so only logged-in users can access it
    [Authorize] // You might need to specify roles later, e.g., [Authorize(Roles = "Reviewer")]
    public class ReviewerHomeController : Controller
    {
        // --- Constructor ---
        // Add a constructor here if you need to inject services (like DB access, etc.)
        // Example:
        // private readonly YourDbContext _context;
        // public ReviewerHomeController(YourDbContext context)
        // {
        //     _context = context;
        // }

        // --- Index Action ---
        // FIX: Create and pass a ViewModel to the View
        public IActionResult Index()
        {
            // TODO: Replace this with actual logic to get data for the ViewModel
            // Get current user information if needed
            // string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Gets the logged-in user's ID
            // string username = User.Identity.Name; // Gets the logged-in user's username

            // Create an instance of the ViewModel the Index.cshtml view expects
            // Make sure ReviewerHomeViewModel class exists in Models/ViewModels
            var viewModel = new ReviewerHomeViewModel
            {
                // Populate the ViewModel properties with data needed by the view
                // Example:
                // ReviewerName = username,
                // RecentReviews = _context.Reviews.Where(r => r.UserId == userId).OrderByDescending(r => r.DateCreated).Take(5).ToList(),
                // PendingActionsCount = ... // etc.
            };

            // Pass the populated ViewModel to the View
            return View(viewModel);
        }

        // --- ManageReviews Action ---
        // This action will handle GET requests to /ReviewerHome/ManageReviews
        [HttpGet] // Explicitly marking as HttpGet (optional if no other verb is present, but clear)
        public IActionResult ManageReviews()
        {
            // TODO: Add logic here to fetch the reviews written by the currently logged-in reviewer
            // You might need to get the user's ID: User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Then query your database/API for reviews associated with that user ID.
            // Example: var reviews = _context.Reviews.Where(r => r.UserId == userId).ToList();
            // Example: var viewModel = new ManageReviewsViewModel { Reviews = reviews };

            // Return the corresponding View. Make sure you have a ManageReviews.cshtml view
            // in the Views/ReviewerHome folder.
            // If this view also needs a model, create and pass it like in the Index action.
            return View(); // Or return View(manageReviewsViewModel);
        }

        // Add other actions needed for the Reviewer Home section here...

    }
}
