using Microsoft.AspNetCore.Mvc;
// Using organized namespaces - ensure these match your project
using Project3.Models.ViewModels;
using Project3.Models.InputModels; // For SearchCriteriaViewModel
// using Project3.Models.DTOs; // Add if API returns specific DTOs
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq; // For Any()
using System; // For Uri, Exception
using System.Xml.Linq;

namespace Project3.Controllers
{
    [Authorize(Roles = "reviewer")] // Only reviewers access this
    public class ReviewerHomeController : Controller
    {
        private readonly ILogger<ReviewerHomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ReviewerHomeController(ILogger<ReviewerHomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /ReviewerHome/Index
        // Displays the main dashboard, including featured restaurants or search results.
        // Accepts viewModel from Search POST to display results, otherwise loads featured.
        [HttpGet] // Explicitly mark GET
        public async Task<IActionResult> Index(ReviewerHomeViewModel? viewModel = null) // Use nullable reference type
        {
            bool loadFeatured = false;
            if (viewModel == null) // Initial GET request
            {
                viewModel = new ReviewerHomeViewModel();
                loadFeatured = true;
            }
            else if (viewModel.SearchResults == null) // Model passed but no results (e.g., error during search?)
            {
                viewModel.SearchResults = new List<RestaurantViewModel>();
                loadFeatured = true; // Show featured if search failed or wasn't performed
            }
            // If viewModel.SearchResults is NOT null, Search action populated it.

            // --- API Call to get Available Cuisines (always needed for filter) ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update API endpoint URL for distinct cuisines
                string cuisinesApiUrl = "api/restaurants/cuisines"; // Example URL
                _logger.LogDebug("Calling API GET {ApiUrl} for cuisines", cuisinesApiUrl);

                viewModel.AvailableCuisines = await client.GetFromJsonAsync<List<string>>(cuisinesApiUrl) ?? new List<string>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API call failed: Could not get available cuisines. Status Code: {StatusCode}", ex.StatusCode);
                viewModel.AvailableCuisines = new List<string>(); // Ensure empty list on error
                TempData["ErrorMessage"] = "Could not load cuisine filter options.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available cuisines.");
                viewModel.AvailableCuisines = new List<string>();
                TempData["ErrorMessage"] = "An error occurred loading cuisine filter options.";
            }
            // --- End API Call ---


            // --- API Call to get Featured Restaurants (only if needed) ---
            if (loadFeatured)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api");
                    // TODO: Verify/Update API endpoint URL for featured restaurants
                    string featuredApiUrl = "api/restaurants/featured?topN=6"; // Example URL
                    _logger.LogDebug("Calling API GET {ApiUrl} for featured restaurants", featuredApiUrl);

                    // TODO: API might return a DTO (e.g., List<RestaurantSearchResultDto>) that needs mapping
                    // For now, assuming it returns List<RestaurantViewModel>
                    viewModel.FeaturedRestaurants = await client.GetFromJsonAsync<List<RestaurantViewModel>>(featuredApiUrl) ?? new List<RestaurantViewModel>();
                    _logger.LogInformation("Loaded {Count} featured restaurants.", viewModel.FeaturedRestaurants.Count);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "API call failed: Could not get featured restaurants. Status Code: {StatusCode}", ex.StatusCode);
                    viewModel.FeaturedRestaurants = new List<RestaurantViewModel>(); // Ensure empty list
                    TempData["ErrorMessage"] = (TempData["ErrorMessage"]?.ToString() ?? "") + " Could not load featured restaurants.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading featured restaurants.");
                    viewModel.FeaturedRestaurants = new List<RestaurantViewModel>();
                    TempData["ErrorMessage"] = (TempData["ErrorMessage"]?.ToString() ?? "") + " An error occurred loading featured restaurants.";
                }
            }
            // --- End API Call ---

            ViewData["Username"] = User.Identity?.Name ?? "Reviewer";
            return View(viewModel); // Pass model to Views/ReviewerHome/Index.cshtml
        }

        // POST: /ReviewerHome/Search
        // Handles the search form submission, calls the search API, and redisplays Index with results.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(ReviewerHomeViewModel viewModel) // Model binding populates viewModel.SearchCriteria
        {
            if (viewModel?.SearchCriteria == null)
            {
                // Should not happen with proper form submission, but handle defensively
                _logger.LogWarning("Search POST received null SearchCriteria.");
                return RedirectToAction(nameof(Index));
            }

            var searchResults = new List<RestaurantViewModel>();
            // --- API Call to Search Restaurants ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // Build query string for API call based on criteria
                // TODO: Verify/Update Restaurant Search API GET endpoint URL
                string searchBaseUrl = "api/restaurants/search"; // Example URL
                string apiUrl = BuildSearchApiUrl(searchBaseUrl, viewModel.SearchCriteria);
                _logger.LogDebug("Calling API GET {ApiUrl} for search", apiUrl);


                // TODO: API might return a DTO (e.g., List<RestaurantSearchResultDto>) that needs mapping
                // For now, assuming it returns List<RestaurantViewModel>
                searchResults = await client.GetFromJsonAsync<List<RestaurantViewModel>>(apiUrl) ?? new List<RestaurantViewModel>();
                _logger.LogInformation("Search API returned {Count} restaurants.", searchResults.Count);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API call failed: Could not search restaurants. Status Code: {StatusCode}", ex.StatusCode);
                TempData["ErrorMessage"] = "An error occurred during the search.";
                // Keep searchResults as empty list
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching restaurants.");
                TempData["ErrorMessage"] = "An unexpected error occurred during the search.";
            }
            // --- End API Call ---

            // Prepare the ViewModel to pass back to the Index action
            // Include search criteria (to repopulate form) and search results
            var resultsViewModel = new ReviewerHomeViewModel
            {
                SearchCriteria = viewModel.SearchCriteria,
                SearchResults = searchResults
                // AvailableCuisines will be reloaded by the Index action when called below
            };

            // Call the Index action again, passing the ViewModel containing the search results and criteria.
            // This avoids duplicating the logic for loading cuisines and setting up the view.
            return await Index(resultsViewModel);
        }

        // --- Helper Method ---
        // NOTE: Consider moving this URL building logic to a shared utility/service class.
        private string BuildSearchApiUrl(string baseUrl, SearchCriteriaViewModel criteria)
        {
            var queryParams = new Dictionary<string, string>(); // Use Dictionary for easier handling

            if (!string.IsNullOrWhiteSpace(criteria.City))
            {
                queryParams["city"] = criteria.City;
            }
            if (!string.IsNullOrWhiteSpace(criteria.State))
            {
                queryParams["state"] = criteria.State;
            }
            if (!string.IsNullOrWhiteSpace(criteria.CuisineInput)) // Assuming CuisineInput is comma-separated string
            {
                // API needs to handle parsing this comma-separated list
                queryParams["cuisines"] = criteria.CuisineInput;
            }
            // TODO: Add other potential search criteria here (e.g., Name, ZipCode)

            if (!queryParams.Any())
            {
                return baseUrl; // No parameters
            }

            // Use QueryHelpers for robust query string building (requires Microsoft.AspNetCore.WebUtilities)
            // Or manually build:
            var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{baseUrl}?{queryString}";
        }

        // Redirect Actions like ManageReviews, AddReview, Logout are handled by Links/Forms in the View using Tag Helpers,
        // pointing directly to the appropriate Controller/Action (e.g., ReviewController.Index, ReviewController.Create, AccountController.Logout).

    }
}