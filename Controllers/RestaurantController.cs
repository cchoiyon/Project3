using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json; // For GetFromJsonAsync
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Project3.Models.Domain; // For Restaurant, Photo
using Project3.Models.ViewModels; // For RestaurantDetailViewModel, ReviewViewModel, SearchCriteriaViewModel
// using Project3.Models.DTOs; // Add if using DTOs for API calls
using System; // For Exception, Math
using System.Linq; // For Any(), Average()
using Project3.Models.InputModels;
using System.Threading.Channels;
using System.Xml.Linq;
using Project3.Utilities;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory; // Add for caching

namespace Project3.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ILogger<RestaurantController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DBConnect _dbConnect;
        private readonly IMemoryCache _cache; // Add cache field
        private const string CuisinesCacheKey = "AllCuisines"; // Cache key for cuisines

        public RestaurantController(
            ILogger<RestaurantController> logger, 
            IHttpClientFactory httpClientFactory, 
            DBConnect dbConnect,
            IMemoryCache cache) // Add cache to constructor
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbConnect = dbConnect;
            _cache = cache;
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

        // Add private method to get cuisines
        private List<string> GetCuisinesList()
        {
            // Try to get cuisines from cache first
            if (_cache.TryGetValue(CuisinesCacheKey, out List<string> cachedCuisines))
            {
                return cachedCuisines;
            }

            var cuisines = new List<string>();
            try
            {
                var cmd = new SqlCommand("dbo.TP_spGetAllCuisines");
                cmd.CommandType = CommandType.StoredProcedure;
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows != null)
                {
                    cuisines = ds.Tables[0].Rows
                        .Cast<DataRow>()
                        .Where(row => row["Cuisine"] != DBNull.Value)
                        .Select(row => row["Cuisine"].ToString())
                        .Where(cuisine => !string.IsNullOrWhiteSpace(cuisine))
                        .ToList();

                    // Cache the cuisines for 1 hour
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    _cache.Set(CuisinesCacheKey, cuisines, cacheOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cuisines list");
            }

            return cuisines;
        }

        // GET: /Restaurant/Search
        public IActionResult Search()
        {
            var model = new SearchCriteriaViewModel
            {
                AvailableCuisines = GetCuisinesList()
            };
            return View(model);
        }

        // POST: /Restaurant/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search(SearchCriteriaViewModel searchCriteria)
        {
            // Initialize if null
            searchCriteria ??= new SearchCriteriaViewModel();
            
            // Always repopulate the cuisines list
            searchCriteria.AvailableCuisines = GetCuisinesList();

            try
            {
                var searchResults = new List<RestaurantViewModel>();
                var cmd = new SqlCommand("dbo.TP_spSearchRestaurants");
                cmd.CommandType = CommandType.StoredProcedure;
                
                // Add parameters only if they have values, otherwise use DBNull.Value
                cmd.Parameters.AddWithValue("@CuisineList", 
                    !string.IsNullOrWhiteSpace(searchCriteria.CuisineInput) 
                        ? searchCriteria.CuisineInput.Trim() 
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@City", 
                    !string.IsNullOrWhiteSpace(searchCriteria.City) 
                        ? searchCriteria.City.Trim() 
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@State", 
                    !string.IsNullOrWhiteSpace(searchCriteria.State) 
                        ? searchCriteria.State.Trim().ToUpper() 
                        : (object)DBNull.Value);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        var restaurant = new RestaurantViewModel
                        {
                            RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                            Name = row["Name"]?.ToString() ?? string.Empty,
                            Cuisine = row["Cuisine"]?.ToString() ?? string.Empty,
                            City = row["City"]?.ToString() ?? string.Empty,
                            State = row["State"]?.ToString() ?? string.Empty,
                            Address = row["Address"]?.ToString() ?? string.Empty,
                            ProfilePhoto = row["LogoPhoto"]?.ToString() ?? string.Empty,
                            AverageRating = row["OverallRating"] != DBNull.Value ? Convert.ToDouble(row["OverallRating"]) : 0,
                            ReviewCount = row["ReviewCount"] != DBNull.Value ? Convert.ToInt32(row["ReviewCount"]) : 0,
                            AveragePriceLevel = row["AveragePriceRating"] != DBNull.Value ? Convert.ToDouble(row["AveragePriceRating"]) : 0
                        };
                        searchResults.Add(restaurant);
                    }
                }

                // Store results in TempData to persist across redirect
                TempData["SearchResults"] = System.Text.Json.JsonSerializer.Serialize(searchResults);
                return RedirectToAction(nameof(SearchResults));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching restaurants");
                ModelState.AddModelError("", "An error occurred while searching restaurants. Please try again.");
                return View(searchCriteria);
            }
        }

        // GET: /Restaurant/SearchResults
        public IActionResult SearchResults()
        {
            var model = new SearchCriteriaViewModel
            {
                AvailableCuisines = GetCuisinesList()
            };

            if (TempData["SearchResults"] != null)
            {
                try
                {
                    ViewBag.SearchResults = System.Text.Json.JsonSerializer.Deserialize<List<RestaurantViewModel>>(
                        TempData["SearchResults"].ToString());
                    
                    // Preserve the search results for the next request since we're using them in the view
                    TempData.Keep("SearchResults");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing search results");
                    ViewBag.SearchResults = new List<RestaurantViewModel>();
                }
            }
            else
            {
                ViewBag.SearchResults = new List<RestaurantViewModel>();
            }

            return View(model);
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