using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json; // For GetFromJsonAsync
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Project3.Models.Domain; // For Restaurant, Photo
using Project3.Models.ViewModels; // For RestaurantDetailViewModel, ReviewViewModel
// using Project3.Models.DTOs; // Add if using DTOs for API calls
using System; // For Exception, Math
using System.Linq; // For Any(), Average()
using Project3.Models.InputModels;
using System.Threading.Channels;
using System.Xml.Linq;

namespace Project3.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ILogger<RestaurantController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RestaurantController(ILogger<RestaurantController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Restaurant/Details/5
        // Fetches profile, reviews, and photos from APIs to display details.
        public async Task<IActionResult> Details(int id) // id is RestaurantID
        {
            if (id <= 0)
            {
                _logger.LogWarning("Details requested with invalid ID: {RestaurantId}", id);
                return NotFound(); // Return 404 for invalid ID
            }

            // Use the ViewModel designed for this page
            var viewModel = new RestaurantDetailViewModel { RestaurantID = id };

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api"); // Use named client

                // --- API Call 1: Get Restaurant Profile ---
                // TODO: Verify/Update Restaurant API GET endpoint URL
                string profileApiUrl = $"api/restaurants/{id}"; // Example URL
                _logger.LogDebug("Calling API GET {ApiUrl}", profileApiUrl);
                var profileTask = client.GetFromJsonAsync<Restaurant>(profileApiUrl);

                // --- API Call 2: Get Restaurant Reviews ---
                // TODO: Verify/Update Review API GET endpoint URL (filtered by restaurant)
                string reviewsApiUrl = $"api/reviews/restaurant/{id}"; // Example URL
                _logger.LogDebug("Calling API GET {ApiUrl}", reviewsApiUrl);
                var reviewsTask = client.GetFromJsonAsync<List<ReviewViewModel>>(reviewsApiUrl); // Assuming API returns ReviewViewModel

                // --- API Call 3: Get Restaurant Photos ---
                // TODO: Verify/Update Photo API GET endpoint URL (filtered by restaurant)
                // Assuming API endpoint is part of RestaurantsApi or a separate PhotosApi
                string photosApiUrl = $"api/restaurants/{id}/photos"; // Example URL within RestaurantsApi
                                                                      // string photosApiUrl = $"api/photos/restaurant/{id}"; // Example URL for separate PhotosApi
                _logger.LogDebug("Calling API GET {ApiUrl}", photosApiUrl);
                // *** FIXED: Deserialize into List<Photo> (domain model) ***
                var photosTask = client.GetFromJsonAsync<List<Photo>>(photosApiUrl);

                // Await all tasks concurrently
                await Task.WhenAll(profileTask, reviewsTask, photosTask);

                // Process results
                viewModel.Profile = await profileTask;
                // Use ?? new List... to handle potential null return from API gracefully
                viewModel.Reviews = await reviewsTask ?? new List<ReviewViewModel>();
                // *** FIXED: Assign to List<Photo> and handle null ***
                viewModel.Photos = await photosTask ?? new List<Photo>();

                if (viewModel.Profile == null)
                {
                    _logger.LogWarning("Restaurant profile not found via API for ID {RestaurantId}", id);
                    return NotFound(); // Restaurant doesn't exist
                }

                // Calculate display values using helper methods
                // Consider moving these calculations into the ViewModel itself or a service
                viewModel.AverageRatingDisplay = GetStars(CalculateAverageRating(viewModel.Reviews));
                viewModel.AveragePriceLevelDisplay = GetPriceLevel(CalculateAveragePriceLevel(viewModel.Reviews));

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API call failed: Could not get details for Restaurant ID {RestaurantId}. Status Code if available: {StatusCode}", id, ex.StatusCode);
                TempData["ErrorMessage"] = "Could not load restaurant details at this time. Please try again later.";
                // Redirecting to home might be confusing, consider showing an error view or message on current page?
                // For now, keeping redirect to Home
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) // Catch other potential errors (e.g., Task.WhenAll failures)
            {
                _logger.LogError(ex, "Error loading details for Restaurant ID {RestaurantId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred loading restaurant details.";
                return RedirectToAction("Index", "Home");
            }

            // Pass the populated ViewModel to the View
            return View(viewModel); // Views/Restaurant/Details.cshtml
        }

        // --- Helper methods ---
        // NOTE: Consider moving these calculation/formatting helpers into the
        // RestaurantDetailViewModel class or a dedicated utility/service class
        // to improve separation of concerns and align better with component design.

        private double CalculateAverageRating(List<ReviewViewModel> reviews)
        {
            if (reviews == null || !reviews.Any()) return 0;
            // Assuming ReviewViewModel has FoodQualityRating, ServiceRating, AtmosphereRating
            try
            {
                // Using Average() handles potential empty list implicitly if called after Any() check
                return reviews.Average(r => (r.FoodQualityRating + r.ServiceRating + r.AtmosphereRating) / 3.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating.");
                return 0; // Return default on error
            }
        }

        private int CalculateAveragePriceLevel(List<ReviewViewModel> reviews)
        {
            if (reviews == null || !reviews.Any()) return 0;
            // Assuming ReviewViewModel has PriceRating
            try
            {
                // Using Average() handles potential empty list implicitly if called after Any() check
                return (int)Math.Round(reviews.Average(r => r.PriceRating));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average price level.");
                return 0; // Return default on error
            }
        }

        private string GetStars(double rating)
        {
            // Example: Return simple text - View should handle rendering actual stars
            return $"{Math.Round(rating, 1)} / 5";
        }

        private string GetPriceLevel(int priceLevel)
        {
            // Example: Return dollar signs - View can use this directly
            priceLevel = Math.Max(1, Math.Min(5, priceLevel)); // Ensure 1-5
            return new string('$', priceLevel);
        }
        // --- End Helper Methods ---
    }
}