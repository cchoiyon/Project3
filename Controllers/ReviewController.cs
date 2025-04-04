using Microsoft.AspNetCore.Mvc;
// Using organized namespaces - ensure these match your project
using Project3.Models.ViewModels;
using Project3.Models.Domain;
using Project3.Models.DTOs; // Add using for your API DTOs
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization; // Require login
using System.Collections.Generic; // For List
using System.Net; // For HttpStatusCode
using System; // For DateTime, Exception
using System.Linq; // For Any()
using Microsoft.AspNetCore.Http.HttpResults;
using Project3.Models.InputModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace Project3.Controllers
{
    [Authorize(Roles = "reviewer")] // Only reviewers can manage reviews
    public class ReviewController : Controller
    {
        private readonly ILogger<ReviewController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ReviewController(ILogger<ReviewController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Review/Index (List user's reviews)
        // Fetches reviews for the current user from the API.
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Review Index: UserID claim is missing.");
                return Unauthorized("User identifier is missing.");
            }

            List<ReviewViewModel> myReviews = new List<ReviewViewModel>();
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api"); // Use named client
                // TODO: Verify/Update Review API GET endpoint URL (filtered by user)
                string apiUrl = $"api/reviews/user/{userId}"; // Example URL
                _logger.LogDebug("Calling API GET {ApiUrl}", apiUrl);

                // Assuming API returns ReviewViewModel directly or a DTO mappable to it
                myReviews = await client.GetFromJsonAsync<List<ReviewViewModel>>(apiUrl) ?? new List<ReviewViewModel>();
                _logger.LogInformation("Loaded {Count} reviews for User ID {UserId}", myReviews.Count, userId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API call failed: Could not get reviews for User ID {UserId}. Status Code: {StatusCode}", userId, ex.StatusCode);
                TempData["ErrorMessage"] = "Could not load your reviews at this time.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews for User ID {UserId}", userId);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading reviews.";
            }

            return View(myReviews); // Pass list to Views/Review/Index.cshtml
        }

        // GET: /Review/Create?restaurantId=123
        // Displays the form to create a new review for a specific restaurant.
        public async Task<IActionResult> Create(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                _logger.LogWarning("Review Create GET: Invalid Restaurant ID {RestaurantId}", restaurantId);
                return BadRequest("Invalid Restaurant ID.");
            }

            // Use the base Review domain model for form binding in this case
            var viewModel = new Review
            {
                RestaurantID = restaurantId,
                VisitDate = DateTime.Today // Default visit date
            };

            // --- API Call to get Restaurant Name ---
            string? restaurantName = await GetRestaurantNameAsync(restaurantId);
            if (restaurantName == null)
            {
                // Error logged in helper method
                TempData["ErrorMessage"] = "Could not load restaurant details to start review.";
                return RedirectToAction(nameof(Index)); // Or redirect to restaurant search
            }
            ViewData["RestaurantName"] = restaurantName; // Pass name via ViewData
            // --- End API Call ---

            return View(viewModel); // Pass model to Views/Review/Create.cshtml
        }

        // POST: /Review/Create
        // Submits the new review data to the API.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model) // Bind form to Review model
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authenticatedUserId))
            {
                _logger.LogWarning("Review Create POST: UserID claim is missing or invalid.");
                return Unauthorized("User identifier is missing or invalid.");
            }

            // model.UserID = authenticatedUserId; // UserID should be set by API based on authenticated user

            // --- Manual Validation (supplements Model Annotations if Review model has them) ---
            if (model.VisitDate > DateTime.Today) ModelState.AddModelError(nameof(model.VisitDate), "Visit date cannot be in the future.");
            if (model.FoodQualityRating < 1 || model.FoodQualityRating > 5) ModelState.AddModelError(nameof(model.FoodQualityRating), "Food Quality rating must be 1-5.");
            if (model.ServiceRating < 1 || model.ServiceRating > 5) ModelState.AddModelError(nameof(model.ServiceRating), "Service rating must be 1-5.");
            if (model.AtmosphereRating < 1 || model.AtmosphereRating > 5) ModelState.AddModelError(nameof(model.AtmosphereRating), "Atmosphere rating must be 1-5.");
            if (model.PriceRating < 1 || model.PriceRating > 5) ModelState.AddModelError(nameof(model.PriceRating), "Price Level rating must be 1-5.");
            if (string.IsNullOrWhiteSpace(model.Comments)) ModelState.AddModelError(nameof(model.Comments), "Comments are required.");
            // --- End Validation ---

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Review Create POST failed validation for Restaurant {RestaurantId}", model.RestaurantID);
                // ** Repopulate Restaurant Name if returning View **
                // Consider separate InputModel to avoid this re-fetch
                ViewData["RestaurantName"] = await GetRestaurantNameAsync(model.RestaurantID) ?? "Selected Restaurant";
                return View(model); // Return view with validation errors
            }

            // --- Prepare DTO for API Call ---
            // TODO: Define CreateReviewDto in Models/DTOs
            // Map data from the input 'model' to the DTO
            var reviewDto = new CreateReviewDto
            {
                RestaurantID = model.RestaurantID,
                VisitDate = model.VisitDate,
                Comments = model.Comments,
                FoodQualityRating = model.FoodQualityRating,
                ServiceRating = model.ServiceRating,
                AtmosphereRating = model.AtmosphereRating,
                PriceRating = model.PriceRating
                // UserID will be determined by the API from the authenticated user context
            };
            // --- End DTO Preparation ---


            // --- API Call to add Review ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update Review API POST endpoint URL
                string apiUrl = "api/reviews"; // Example URL
                _logger.LogDebug("Calling API POST {ApiUrl} to add review", apiUrl);

                var response = await client.PostAsJsonAsync(apiUrl, reviewDto); // Send DTO

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Review added successfully via API for Restaurant {RestaurantId} by User {UserId}", model.RestaurantID, authenticatedUserId);
                    TempData["SuccessMessage"] = "Review added successfully!";
                    return RedirectToAction(nameof(Index)); // Redirect to review list
                }
                else // Handle API errors
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                    _logger.LogError("API call failed: Could not add review. Status: {StatusCode}, Reason: {ApiError}",
                       response.StatusCode, errorResponse?.Message ?? await response.Content.ReadAsStringAsync());
                    ModelState.AddModelError(string.Empty, errorResponse?.Message ?? "Error adding review. Please try again.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error: Could not add review for Restaurant {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError(string.Empty, "Error adding review due to a connection issue.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding review for Restaurant {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while adding the review.");
            }
            // --- End API Call ---

            // If API call failed, return the view with the model and errors
            ViewData["RestaurantName"] = await GetRestaurantNameAsync(model.RestaurantID) ?? "Selected Restaurant";
            return View(model);
        }

        // GET: /Review/Edit/5
        // Displays the form to edit an existing review.
        public async Task<IActionResult> Edit(int id) // id = ReviewID
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authenticatedUserId))
            {
                _logger.LogWarning("Review Edit GET: UserID claim is missing or invalid.");
                return Unauthorized("User identifier is missing or invalid.");
            }

            Review model = null;
            // --- API Call to get Review Details ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update Review API GET endpoint for a single review
                string apiUrl = $"api/reviews/{id}"; // Example URL
                _logger.LogDebug("Calling API GET {ApiUrl}", apiUrl);

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Assuming API returns the Review domain model or a DTO mappable to it
                    model = await response.Content.ReadFromJsonAsync<Review>();
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Review Edit GET: Review {ReviewId} not found by API.", id);
                    return NotFound();
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // API might do ownership check here
                    _logger.LogWarning("Review Edit GET: User {UserId} forbidden by API from getting Review {ReviewId}.", userId, id);
                    TempData["ErrorMessage"] = "You do not have permission to view this review for editing.";
                    return RedirectToAction(nameof(Index));
                }
                else // Handle other API errors
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed: Could not get review {ReviewId}. Status: {StatusCode}, Content: {ErrorContent}",
                       id, response.StatusCode, errorContent);
                    TempData["ErrorMessage"] = "Error loading review details.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error: Could not get review {ReviewId}", id);
                TempData["ErrorMessage"] = "Error loading review details due to connection issue.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting review {ReviewId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred loading review details.";
                return RedirectToAction(nameof(Index));
            }
            // --- End API Call ---

            if (model == null) return NotFound(); // Should be caught above, but safety check

            // Security Check: Ensure the logged-in user owns this review
            if (model.UserID != authenticatedUserId)
            {
                _logger.LogWarning("User {UserId} attempted to edit review {ReviewId} owned by User {OwnerId}", userId, id, model.UserID);
                TempData["ErrorMessage"] = "You can only edit your own reviews.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["RestaurantName"] = await GetRestaurantNameAsync(model.RestaurantID) ?? "Selected Restaurant";
            return View(model); // Pass model to Views/Review/Edit.cshtml
        }

        // POST: /Review/Edit/5
        // Submits the updated review data to the API.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Review model) // id from route, model from form
        {
            if (id != model.ReviewID) return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int authenticatedUserId))
            {
                _logger.LogWarning("Review Edit POST: UserID claim is missing or invalid.");
                return Unauthorized("User identifier is missing or invalid.");
            }

            // Ensure the UserID in the model matches the authenticated user for security
            // Although the API should perform the definitive check.
            if (model.UserID != authenticatedUserId)
            {
                _logger.LogWarning("Review Edit POST: Model UserID {ModelUserId} does not match authenticated user {AuthUserId}.", model.UserID, authenticatedUserId);
                return Forbid(); // Or BadRequest
            }

            // --- Manual Validation (Similar to Create) ---
            if (model.VisitDate > DateTime.Today) ModelState.AddModelError(nameof(model.VisitDate), "Visit date cannot be in the future.");
            // Add other checks...
            // --- End Validation ---

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Review Edit POST failed validation for Review {ReviewId}", model.ReviewID);
                // ** Repopulate Restaurant Name if returning View **
                ViewData["RestaurantName"] = await GetRestaurantNameAsync(model.RestaurantID) ?? "Selected Restaurant";
                return View(model); // Return view with validation errors
            }

            // --- Prepare DTO for API Call ---
            // TODO: Define UpdateReviewDto in Models/DTOs
            // Map data from the input 'model' to the DTO
            var reviewDto = new UpdateReviewDto
            {
                // Include only fields that should be updated
                VisitDate = model.VisitDate,
                Comments = model.Comments,
                FoodQualityRating = model.FoodQualityRating,
                ServiceRating = model.ServiceRating,
                AtmosphereRating = model.AtmosphereRating,
                PriceRating = model.PriceRating
                // API will use the 'id' from the route and verify ownership against authenticated user
            };
            // --- End DTO Preparation ---


            // --- API Call to update Review ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update Review API PUT endpoint URL
                string apiUrl = $"api/reviews/{id}"; // Example URL
                _logger.LogDebug("Calling API PUT {ApiUrl} to update review", apiUrl);

                var response = await client.PutAsJsonAsync(apiUrl, reviewDto); // Send DTO

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Review {ReviewId} updated successfully via API by User {UserId}", model.ReviewID, authenticatedUserId);
                    TempData["SuccessMessage"] = "Review updated successfully!";
                    return RedirectToAction(nameof(Index)); // Redirect to review list
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Review Edit POST: Review {ReviewId} not found by API.", id);
                    return NotFound(); // Or redirect to Index with message
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("Review Edit POST: User {UserId} forbidden by API from updating Review {ReviewId}.", userId, id);
                    TempData["ErrorMessage"] = "You do not have permission to edit this review.";
                    return RedirectToAction(nameof(Index));
                }
                else // Handle other API errors
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                    _logger.LogError("API call failed: Could not update review {ReviewId}. Status: {StatusCode}, Reason: {ApiError}",
                       id, response.StatusCode, errorResponse?.Message ?? await response.Content.ReadAsStringAsync());
                    ModelState.AddModelError(string.Empty, errorResponse?.Message ?? "Error updating review. Please try again.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error: Could not update review {ReviewId}", id);
                ModelState.AddModelError(string.Empty, "Error updating review due to connection issue.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating review {ReviewId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the review.");
            }
            // --- End API Call ---

            // If API call failed, return the view with the model and errors
            ViewData["RestaurantName"] = await GetRestaurantNameAsync(model.RestaurantID) ?? "Selected Restaurant";
            return View(model);
        }


        // POST: /Review/Delete/5
        // Deletes a review via API call.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) // id = ReviewID
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            _logger.LogInformation("User {UserId} attempting to delete review {ReviewId}", userId, id);

            // Optional: Pre-check ownership via API GET before calling DELETE
            // This provides quicker feedback if user doesn't own it, but adds an extra API call.
            // Relying on API's DELETE endpoint to check ownership is also valid.

            // --- API Call to delete Review ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update Review API DELETE endpoint URL
                string apiUrl = $"api/reviews/{id}"; // Example URL
                _logger.LogDebug("Calling API DELETE {ApiUrl}", apiUrl);

                // API endpoint should verify ownership based on authenticated user
                var response = await client.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Review {ReviewId} deleted successfully via API by User {UserId}", id, userId);
                    TempData["SuccessMessage"] = "Review deleted successfully.";
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Review Delete POST: Review {ReviewId} not found by API.", id);
                    TempData["ErrorMessage"] = "Review not found.";
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogWarning("Review Delete POST: User {UserId} forbidden by API from deleting Review {ReviewId}.", userId, id);
                    TempData["ErrorMessage"] = "You do not have permission to delete this review.";
                }
                else // Handle other API errors
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                    _logger.LogError("API call failed: Could not delete review {ReviewId}. Status: {StatusCode}, Reason: {ApiError}",
                       id, response.StatusCode, errorResponse?.Message ?? await response.Content.ReadAsStringAsync());
                    TempData["ErrorMessage"] = errorResponse?.Message ?? "Error deleting review. Please try again.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error: Could not delete review {ReviewId}", id);
                TempData["ErrorMessage"] = "Error deleting review due to connection issue.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting review {ReviewId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the review.";
            }
            // --- End API Call ---

            return RedirectToAction(nameof(Index)); // Redirect back to the list
        }

        // Helper to get restaurant name (used by Create/Edit views)
        private async Task<string?> GetRestaurantNameAsync(int restaurantId)
        {
            if (restaurantId <= 0) return null;
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update Restaurant API GET endpoint URL
                // TODO: Consider fetching a simpler DTO if only Name is needed
                string apiUrl = $"api/restaurants/{restaurantId}"; // Example URL
                _logger.LogDebug("Calling API GET {ApiUrl} for restaurant name helper", apiUrl);
                var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>(apiUrl); // Assuming RestaurantViewModel has Name
                return restaurant?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Helper API call failed: Could not get restaurant name for ID {RestaurantId}", restaurantId);
                return null; // Return null on error
            }
        }

        // --- TODO: Define DTOs properly in Models/DTOs folder ---
        // Example DTO classes (replace with actual definitions in separate files)
        private record CreateReviewDto
        {
            public int RestaurantID { get; set; }
            public DateTime VisitDate { get; set; }
            public string Comments { get; set; }
            public int FoodQualityRating { get; set; }
            public int ServiceRating { get; set; }
            public int AtmosphereRating { get; set; }
            public int PriceRating { get; set; }
        }
        private record UpdateReviewDto
        {
            // May not need ID here if passed in route
            public DateTime VisitDate { get; set; }
            public string Comments { get; set; }
            public int FoodQualityRating { get; set; }
            public int ServiceRating { get; set; }
            public int AtmosphereRating { get; set; }
            public int PriceRating { get; set; }
        }
        // Define RestaurantViewModel if it's different from the one in Models/ViewModels
        // private record RestaurantViewModel(int RestaurantID, string Name /*, other fields */);
        private record ErrorResponseDto(string Message);

    }
}