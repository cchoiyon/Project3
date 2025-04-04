using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project3.Models.Domain;        // For Restaurant, Reservation models
using Project3.Models.DTOs;          // For DTOs like UpdateRestaurantProfileDto, ErrorResponseDto
using Project3.Models.ViewModels;    // For ViewModels (RestaurantViewModel, RestaurantRepHomeViewModel, ReviewViewModel)
using Project3.Models.InputModels; // For ReviewViewModel if it's there
using System;
using System.Collections.Generic;    // For List<>
using System.Linq;                 // For Linq methods if needed
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project3.Controllers
{
    [Authorize(Roles = "restaurantRep")] // Ensure only restaurant reps can access this controller
    public class RestaurantRepHomeController : Controller
    {
        private readonly ILogger<RestaurantRepHomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public RestaurantRepHomeController(ILogger<RestaurantRepHomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /RestaurantRepHome/Index (Dashboard)
        // Updated to fetch data and populate the ViewModel
        public async Task<IActionResult> Index()
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int restaurantId))
            {
                _logger.LogError("Index GET: User identifier claim is invalid or missing for authenticated user.");
                TempData["ErrorMessage"] = "Could not identify your restaurant profile. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Index GET: Loading dashboard data for Restaurant ID {RestaurantId}", restaurantId);

            // Create the ViewModel - properties are initialized in its constructor
            var viewModel = new RestaurantRepHomeViewModel();
            string? errorMessage = null;

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");

                // --- Define API URLs (adjust these based on your actual API routes) ---
                string profileApiUrl = $"api/RestaurantsApi/{restaurantId}";
                // Example: Assumes an endpoint exists to get pending reservations for a restaurant
                string pendingReservationsApiUrl = $"api/ReservationsApi/restaurant/{restaurantId}?status=pending";
                // Example: Assumes an endpoint exists to get recent reviews (e.g., top 5)
                string recentReviewsApiUrl = $"api/ReviewsApi/restaurant/{restaurantId}?recent=true&count=5";

                // --- Create Tasks for Concurrent API Calls ---
                _logger.LogDebug("Index GET: Fetching profile from {ApiUrl}", profileApiUrl);
                var profileTask = client.GetAsync(profileApiUrl);

                _logger.LogDebug("Index GET: Fetching pending reservations from {ApiUrl}", pendingReservationsApiUrl);
                var reservationsTask = client.GetAsync(pendingReservationsApiUrl);

                _logger.LogDebug("Index GET: Fetching recent reviews from {ApiUrl}", recentReviewsApiUrl);
                var reviewsTask = client.GetAsync(recentReviewsApiUrl);

                // --- Await All Tasks ---
                await Task.WhenAll(profileTask, reservationsTask, reviewsTask);

                // --- Process Profile Response ---
                var profileResponse = await profileTask;
                if (profileResponse.IsSuccessStatusCode)
                {
                    var profile = await profileResponse.Content.ReadFromJsonAsync<Restaurant>();
                    if (profile != null)
                    {
                        viewModel.RestaurantProfile = profile; // Assign fetched profile
                        _logger.LogInformation("Index GET: Successfully loaded profile for Restaurant ID {RestaurantId}", restaurantId);
                    }
                    else
                    {
                        _logger.LogWarning("Index GET: Profile API call successful but returned null data for Restaurant ID {RestaurantId}", restaurantId);
                        // Keep default empty profile initialized by ViewModel constructor
                    }
                }
                else if (profileResponse.StatusCode != System.Net.HttpStatusCode.NotFound) // Ignore 404 for profile (means not created yet)
                {
                    _logger.LogError("Index GET: API error loading profile for Restaurant ID {RestaurantId}. Status: {StatusCode}", restaurantId, profileResponse.StatusCode);
                    errorMessage = "Could not load restaurant profile data. "; // Append errors
                }

                // --- Process Reservations Response ---
                var reservationsResponse = await reservationsTask;
                if (reservationsResponse.IsSuccessStatusCode)
                {
                    // Assuming the API returns List<Reservation>
                    var reservations = await reservationsResponse.Content.ReadFromJsonAsync<List<Reservation>>();
                    if (reservations != null)
                    {
                        viewModel.PendingReservations = reservations; // Assign fetched reservations
                        _logger.LogInformation("Index GET: Successfully loaded {Count} pending reservations for Restaurant ID {RestaurantId}", reservations.Count, restaurantId);
                    }
                    else
                    {
                        _logger.LogWarning("Index GET: Reservations API call successful but returned null data for Restaurant ID {RestaurantId}", restaurantId);
                        // Keep default empty list initialized by ViewModel constructor
                    }
                }
                else
                {
                    _logger.LogError("Index GET: API error loading pending reservations for Restaurant ID {RestaurantId}. Status: {StatusCode}", restaurantId, reservationsResponse.StatusCode);
                    errorMessage += "Could not load pending reservations. "; // Append errors
                }

                // --- Process Reviews Response ---
                var reviewsResponse = await reviewsTask;
                if (reviewsResponse.IsSuccessStatusCode)
                {
                    // Assuming the API returns List<ReviewViewModel> (matching the ViewModel property type)
                    var reviews = await reviewsResponse.Content.ReadFromJsonAsync<List<ReviewViewModel>>();
                    if (reviews != null)
                    {
                        viewModel.RecentReviews = reviews; // Assign fetched reviews
                        _logger.LogInformation("Index GET: Successfully loaded {Count} recent reviews for Restaurant ID {RestaurantId}", reviews.Count, restaurantId);
                    }
                    else
                    {
                        _logger.LogWarning("Index GET: Reviews API call successful but returned null data for Restaurant ID {RestaurantId}", restaurantId);
                        // Keep default empty list initialized by ViewModel constructor
                    }
                }
                else
                {
                    _logger.LogError("Index GET: API error loading recent reviews for Restaurant ID {RestaurantId}. Status: {StatusCode}", restaurantId, reviewsResponse.StatusCode);
                    errorMessage += "Could not load recent reviews."; // Append errors
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Index GET: API connection error loading dashboard data for Restaurant ID {RestaurantId}", restaurantId);
                errorMessage = "Could not connect to services to load dashboard data. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index GET: Unexpected error loading dashboard data for Restaurant ID {RestaurantId}", restaurantId);
                errorMessage = "An unexpected error occurred while loading dashboard data.";
            }

            // Pass error message to the view if something went wrong
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ViewData["ErrorMessage"] = errorMessage;
            }

            // Pass the populated (or default) ViewModel to the View
            return View(viewModel); // Now passing the ViewModel object
        }


        // GET: /RestaurantRepHome/ManageProfile
        [HttpGet]
        public async Task<IActionResult> ManageProfile()
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int restaurantId)) // Assuming Rep UserID IS the RestaurantID
            {
                _logger.LogError("ManageProfile GET: User identifier claim is invalid or missing for authenticated user.");
                TempData["ErrorMessage"] = "Could not identify your restaurant profile. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("ManageProfile GET: Loading profile for Restaurant ID {RestaurantId}", restaurantId);

            RestaurantViewModel? viewModel = null; // Use nullable ViewModel
            string? errorMessage = null;

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = $"api/RestaurantsApi/{restaurantId}"; // Use correct API route
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode) // 200 OK - Profile Exists
                {
                    Restaurant? restaurant = await response.Content.ReadFromJsonAsync<Restaurant>();
                    if (restaurant != null)
                    {
                        // *** Map Domain Model to ViewModel ***
                        viewModel = MapRestaurantToViewModel(restaurant);
                        _logger.LogInformation("ManageProfile GET: Successfully loaded and mapped profile for Restaurant ID {RestaurantId}", restaurantId);
                    }
                    else
                    {
                        _logger.LogError("ManageProfile GET: API returned success but failed to deserialize Restaurant object for ID {RestaurantId}", restaurantId);
                        errorMessage = "Error reading profile data. Please try again.";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound) // 404 Not Found - New Profile
                {
                    _logger.LogInformation("ManageProfile GET: No profile found for Restaurant ID {RestaurantId}. Creating new ViewModel.", restaurantId);
                    // Create a new, empty ViewModel, setting the essential RestaurantID
                    viewModel = new RestaurantViewModel
                    {
                        RestaurantID = restaurantId,
                        Name = "" // Initialize required fields if needed
                        // Other properties will be null or default
                    };
                    ViewData["IsNewProfile"] = true; // Flag for the view
                }
                else // Other API error (e.g., 500)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("ManageProfile GET: API error loading profile for Restaurant ID {RestaurantId}. Status: {StatusCode}. Content: {ErrorContent}",
                                     restaurantId, response.StatusCode, errorContent);
                    errorMessage = "Could not load profile data due to a server error. Please try again later.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "ManageProfile GET: API connection error loading profile for Restaurant ID {RestaurantId}", restaurantId);
                errorMessage = "Could not connect to the profile service. Please try again later.";
            }
            catch (Exception ex) // Catch other potential errors (e.g., Task.WhenAll failures)
            {
                _logger.LogError(ex, "ManageProfile GET: Unexpected error loading profile for Restaurant ID {RestaurantId}", restaurantId);
                errorMessage = "An unexpected error occurred. Please try again later.";
            }

            // Handle cases where ViewModel couldn't be created (should be rare if error handling is correct)
            if (viewModel == null && string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = "Could not load or create profile information.";
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                // Use TempData for messages that survive redirects, ViewData for errors displayed on the current view render
                TempData["ErrorMessage"] = errorMessage; // Use TempData if redirecting on error
                ViewData["ErrorMessage"] = errorMessage; // Use ViewData if showing error on the current view
            }

            ViewData["Title"] = (ViewData["IsNewProfile"] as bool? ?? false) ? "Create Restaurant Profile" : "Manage Restaurant Profile";

            // Pass the ViewModel (either populated or new) to the View
            return View(viewModel); // Assumes Views/RestaurantRepHome/ManageProfile.cshtml exists and uses RestaurantViewModel
        }


        // POST: /RestaurantRepHome/ManageProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        // *** Accept RestaurantViewModel from the form ***
        public async Task<IActionResult> ManageProfile(RestaurantViewModel viewModel)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int restaurantId))
            {
                TempData["ErrorMessage"] = "Could not identify your restaurant profile. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Ensure the submitted ViewModel's ID matches the logged-in user's restaurant ID
            if (viewModel.RestaurantID != restaurantId)
            {
                _logger.LogWarning("ManageProfile POST: User {UserId} attempted to submit profile data for different Restaurant ID {ViewModelRestaurantId}.", restaurantId, viewModel.RestaurantID);
                TempData["ErrorMessage"] = "Authorization error updating profile.";
                return RedirectToAction(nameof(ManageProfile)); // Redirect back to GET action
            }

            // *** Validate the ViewModel ***
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ManageProfile POST: Invalid model state for Restaurant ID {RestaurantId}.", restaurantId);
                ViewData["ErrorMessage"] = "Please correct the errors below.";
                ViewData["Title"] = "Manage Restaurant Profile"; // Reset title
                // *** Return the view with the ViewModel to display validation errors ***
                return View(viewModel);
            }

            _logger.LogInformation("ManageProfile POST: Attempting to update profile for Restaurant ID {RestaurantId}", restaurantId);

            // *** Map ViewModel to UpdateRestaurantProfileDto ***
            UpdateRestaurantProfileDto profileDto = MapViewModelToDto(viewModel);

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = $"api/RestaurantsApi/{restaurantId}"; // Use correct API route

                // *** Send the DTO to the API ***
                var response = await client.PutAsJsonAsync(apiUrl, profileDto);

                if (response.IsSuccessStatusCode) // e.g., 204 No Content
                {
                    _logger.LogInformation("ManageProfile POST: Profile updated successfully for Restaurant ID {RestaurantId}", restaurantId);
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(ManageProfile)); // Redirect back to view the updated profile
                }
                else // Handle API errors
                {
                    string apiError = $"API Update Error: Status Code {response.StatusCode}";
                    try { var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(); apiError = errorResponse?.Message ?? apiError; }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from Update Profile API. Status Code: {StatusCode}", response.StatusCode); }

                    _logger.LogError("ManageProfile POST: API error updating profile for Restaurant ID {RestaurantId}. Status: {StatusCode}. Reason: {ApiError}", restaurantId, response.StatusCode, apiError);
                    ModelState.AddModelError(string.Empty, $"Failed to update profile: {apiError}");
                    ViewData["ErrorMessage"] = $"Failed to update profile: {apiError}";
                    ViewData["Title"] = "Manage Restaurant Profile"; // Reset title
                    // *** Return view with ViewModel and error ***
                    return View(viewModel);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "ManageProfile POST: API connection error updating profile for Restaurant ID {RestaurantId}", restaurantId);
                ModelState.AddModelError(string.Empty, "Error connecting to profile service.");
                ViewData["ErrorMessage"] = "Error connecting to profile service.";
                ViewData["Title"] = "Manage Restaurant Profile"; // Reset title
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ManageProfile POST: Unexpected error updating profile for Restaurant ID {RestaurantId}", restaurantId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
                ViewData["ErrorMessage"] = "An unexpected error occurred.";
                ViewData["Title"] = "Manage Restaurant Profile"; // Reset title
                return View(viewModel);
            }
        }

        // --- Helper Methods ---

        private RestaurantViewModel MapRestaurantToViewModel(Restaurant restaurant)
        {
            // Simple manual mapping (Consider AutoMapper for complex scenarios)
            return new RestaurantViewModel
            {
                RestaurantID = restaurant.RestaurantID,
                Name = restaurant.Name,
                Address = restaurant.Address,
                City = restaurant.City,
                State = restaurant.State,
                ZipCode = restaurant.ZipCode,
                Cuisine = restaurant.Cuisine,
                Hours = restaurant.Hours,
                Contact = restaurant.Contact,
                MarketingDescription = restaurant.MarketingDescription,
                WebsiteURL = restaurant.WebsiteURL,
                SocialMedia = restaurant.SocialMedia,
                Owner = restaurant.Owner,
                ProfilePhoto = restaurant.ProfilePhoto, // Assuming these are URLs/paths
                LogoPhoto = restaurant.LogoPhoto, // Assuming these are URLs/paths
                // Calculated fields (OverallRating, ReviewCount, AveragePriceRating)
                // would typically be populated from separate API calls or aggregated data
                // if needed in the ManageProfile view (they aren't usually editable here).
                // Set defaults if not available:
                OverallRating = 0, // Placeholder
                ReviewCount = 0,   // Placeholder
                AveragePriceRating = 0 // Placeholder
            };
        }

        private UpdateRestaurantProfileDto MapViewModelToDto(RestaurantViewModel viewModel)
        {
            // Simple manual mapping
            return new UpdateRestaurantProfileDto
            {
                RestaurantID = viewModel.RestaurantID,
                Name = viewModel.Name,
                Address = viewModel.Address, // Nullable properties map directly
                City = viewModel.City,
                State = viewModel.State,
                ZipCode = viewModel.ZipCode,
                Cuisine = viewModel.Cuisine,
                Hours = viewModel.Hours,
                Contact = viewModel.Contact,
                MarketingDescription = viewModel.MarketingDescription,
                WebsiteURL = viewModel.WebsiteURL,
                SocialMedia = viewModel.SocialMedia,
                Owner = viewModel.Owner,
                ProfilePhoto = viewModel.ProfilePhoto, // Pass URLs/paths
                LogoPhoto = viewModel.LogoPhoto    // Pass URLs/paths
            };
        }

        // TODO: Add actions for managing reviews, photos etc. if needed

    }
}
