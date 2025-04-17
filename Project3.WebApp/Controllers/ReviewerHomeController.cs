using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Needed for [Authorize]
// Add any other necessary using statements for your ViewModels or services
using Project3.Shared.Models.ViewModels; // For ReviewerHomeViewModel
using Project3.Shared.Models.InputModels; // For ReviewViewModel
using Project3.Shared.Utilities; // For Connection class
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims; // Needed for getting user ID potentially
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
// using System.Linq; // If using LINQ for data retrieval

namespace Project3.Controllers
{
    // Ensure the controller requires authorization so only logged-in users can access it
    [Authorize(Roles = "Reviewer")] // Updated to use the correct role name
    public class ReviewerHomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Connection _dbConnect;
        private readonly ILogger<ReviewerHomeController> _logger;

        // --- Constructor ---
        public ReviewerHomeController(IConfiguration configuration, Connection dbConnect, ILogger<ReviewerHomeController> logger)
        {
            _configuration = configuration;
            _dbConnect = dbConnect;
            _logger = logger;
        }

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
            // Get the current user ID
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                TempData["ErrorMessage"] = "You must be logged in to view your reviews.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _logger.LogInformation("Fetching reviews for user ID: {UserId}", currentUserId);
                
                // Query to get all reviews created by the current user with restaurant details
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.*, rst.Name AS RestaurantName
                    FROM TP_Reviews r
                    INNER JOIN TP_Restaurants rst ON r.RestaurantID = rst.RestaurantID
                    WHERE r.UserID = @UserID
                    ORDER BY r.CreatedDate DESC");
                cmd.Parameters.AddWithValue("@UserID", currentUserId);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                // Create list of reviews for the view
                var reviews = new List<ReviewViewModel>();
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        reviews.Add(new ReviewViewModel
                        {
                            ReviewID = Convert.ToInt32(row["ReviewID"]),
                            RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                            RestaurantName = row["RestaurantName"].ToString(),
                            UserID = currentUserId,
                            VisitDate = Convert.ToDateTime(row["VisitDate"]),
                            Comments = row["Comments"].ToString(),
                            FoodQualityRating = Convert.ToInt32(row["FoodQualityRating"]),
                            ServiceRating = Convert.ToInt32(row["ServiceRating"]),
                            AtmosphereRating = Convert.ToInt32(row["AtmosphereRating"]),
                            PriceRating = Convert.ToInt32(row["PriceRating"]),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            ReviewerUsername = User.Identity.Name
                        });
                    }
                    
                    _logger.LogInformation("Found {Count} reviews for user ID {UserId}", reviews.Count, currentUserId);
                }
                else
                {
                    _logger.LogInformation("No reviews found for user ID {UserId}", currentUserId);
                }

                return View(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews for user {UserId}", currentUserId);
                TempData["ErrorMessage"] = "There was an error retrieving your reviews. Please try again.";
                return View(new List<ReviewViewModel>());
            }
        }

        // Helper method to get the current user's ID
        private int GetCurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return 0;
        }

        // Add other actions needed for the Reviewer Home section here...

    }
}
