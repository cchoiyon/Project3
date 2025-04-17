using Microsoft.AspNetCore.Mvc;
// Using organized namespaces - ensure these match your project
using Project3.Shared.Models.ViewModels;
using Project3.Shared.Models.Domain; // For Reservation domain model if used elsewhere
using System.Security.Claims; // For UserID
using System.Net.Http; // For HttpClient
using System.Net.Http.Json; // For GetFromJsonAsync, PostAsJsonAsync etc.
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // For logging
using Microsoft.AspNetCore.Authorization; // AllowAnonymous for guest reservations?
using System.Net; // For HttpStatusCode
using System; // For DateTime
using System.Linq; // If needed for LINQ operations
using System.Threading.Channels;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace Project3.Controllers
{
    // Decide on authorization: Allow anyone to view the form, but maybe require login to POST?
    // Or handle guest logic within POST as currently done.
    // [Authorize] // Uncomment if login is required to even view the form
    public class ReservationController : Controller
    {
        private readonly ILogger<ReservationController> _logger;
        private readonly IHttpClientFactory _httpClientFactory; // Inject HttpClientFactory

        // Local DTO classes to avoid ambiguity
        private class CreateReservationDto
        {
            public int RestaurantID { get; set; }
            public int? UserID { get; set; }
            public DateTime ReservationDateTime { get; set; }
            public int PartySize { get; set; }
            public string ContactName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string SpecialRequests { get; set; }
        }

        private class ErrorResponseDto
        {
            public string Message { get; set; }
        }

        // Constructor to inject dependencies
        public ReservationController(ILogger<ReservationController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Reservation/Create?restaurantId=123
        // Fetches restaurant details and displays the reservation form.
        [HttpGet]
        public async Task<IActionResult> Create(int restaurantId)
        {
            try
            {
                if (restaurantId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid restaurant specified.";
                    return RedirectToAction("Index", "Home"); // Or redirect to restaurant search
                }

                // Use ReservationViewModel to prepare data for the view
                var viewModel = new ReservationViewModel
                {
                    RestaurantID = restaurantId,
                    ReservationDateTime = DateTime.Today.AddDays(1).AddHours(19), // Default Date/Time (19:00)
                    PartySize = 2 // Default Party Size
                };

                // DEMO MODE: Use hardcoded restaurant name without calling API
                viewModel.RestaurantName = $"Demo Restaurant #{restaurantId}";
                ViewBag.DemoMode = "The application is running in demo mode. API calls are bypassed.";
                
                /* API Call is commented out for demo purposes
                // --- API Call to get Restaurant Name (and potentially other needed details) ---
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api"); // Use named client
                    string apiUrl = $"api/restaurants/{restaurantId}";
                    _logger.LogDebug("Calling API GET {ApiUrl} to get restaurant details", apiUrl);

                    // Use a timeout for the API call
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout
                    
                    var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>(apiUrl, cts.Token);

                    if (restaurant != null)
                    {
                        viewModel.RestaurantName = restaurant.Name;
                        apiCallSucceeded = true;
                    }
                    else
                    {
                        _logger.LogWarning("Restaurant ID {RestaurantId} not found by API.", restaurantId);
                        viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "API call timed out: Could not get restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    TempData["WarningMessage"] = "Could not connect to the restaurant details service. Some features may be limited.";
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "API call failed: Could not get restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    TempData["WarningMessage"] = "Could not connect to the restaurant details service. Some features may be limited.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error getting restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    TempData["WarningMessage"] = "An issue occurred while loading restaurant details. Some features may be limited.";
                }
                // --- End API Call ---
                */

                // Pre-fill user info if logged in
                if (User.Identity.IsAuthenticated)
                {
                    // Ensure UserID claim exists and is valid before parsing
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        viewModel.UserID = userId;
                    }
                    viewModel.ContactName = User.Identity.Name; // Or fetch full name if stored in claims
                    viewModel.Email = User.FindFirstValue(ClaimTypes.Email);
                    // viewModel.Phone = User.FindFirstValue(ClaimTypes.MobilePhone); // If available
                }

                return View(viewModel); // Pass the ViewModel to Views/Reservation/Create.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Create GET method for restaurant {RestaurantId}", restaurantId);
                TempData["ErrorMessage"] = "An unexpected system error occurred loading the reservation form.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Reservation/Create
        // Submits the reservation request to the API.
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent CSRF
        // [Authorize] // Uncomment if login is required to POST a reservation
        public async Task<IActionResult> Create(ReservationViewModel model) // Receives data from the form
        {
            // --- Manual Validation (supplements Model Annotations) ---
            if (model.ReservationDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.ReservationDateTime), "Reservation must be for a future date and time.");
            }
            // PartySize validation likely handled by [Range] attribute on ViewModel

            // Guest-specific required fields (if allowing guest reservations)
            if (!User.Identity.IsAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(model.ContactName)) ModelState.AddModelError(nameof(model.ContactName), "Contact Name is required for guest reservations.");
                if (string.IsNullOrWhiteSpace(model.Phone)) ModelState.AddModelError(nameof(model.Phone), "Phone Number is required for guest reservations.");
                if (string.IsNullOrWhiteSpace(model.Email)) ModelState.AddModelError(nameof(model.Email), "Email is required for guest reservations.");
            }
            // --- End Validation ---

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Reservation Create POST failed validation for Restaurant {RestaurantId}", model.RestaurantID);
                // ** Important: Re-populate RestaurantName if returning the View **
                // This requires fetching it again, which is inefficient.
                // Consider the "Post-Redirect-Get" pattern or using a separate InputModel for the POST
                // to avoid needing to reload data just to redisplay the form with errors.
                // For now, we might need to fetch it again:
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api");
                    // TODO: Verify/Update API URL
                    // TODO: Define and use appropriate DTO if API returns something different
                    var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>($"api/restaurants/{model.RestaurantID}");
                    model.RestaurantName = restaurant?.Name ?? "Restaurant"; // Repopulate name
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-fetch restaurant name on validation error for ID {RestaurantId}", model.RestaurantID);
                    model.RestaurantName = "Restaurant"; // Fallback
                }
                return View(model); // Return view with validation errors
            }

            // --- Prepare Data Transfer Object (DTO) for API ---
            var reservationDto = new CreateReservationDto
            {
                RestaurantID = model.RestaurantID,
                // Set UserID only if authenticated
                //UserID = User.Identity.IsAuthenticated ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)) : (int?)null,
                ReservationDateTime = model.ReservationDateTime,
                PartySize = model.PartySize,
                ContactName = model.ContactName,
                Phone = model.Phone,
                Email = model.Email,
                SpecialRequests = model.SpecialRequests
                // API will set Status to 'Pending' and handle CreatedDate
            };
            // --- End DTO Preparation ---

            // --- API Call to submit Reservation ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Verify/Update Reservation API POST endpoint URL
                string apiUrl = "api/reservations"; // Example URL

                _logger.LogDebug("Calling API POST {ApiUrl} to create reservation", apiUrl);
                var response = await client.PostAsJsonAsync(apiUrl, reservationDto); // Send DTO

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Reservation created successfully via API for Restaurant {RestaurantId}", model.RestaurantID);
                    TempData["SuccessMessage"] = "Reservation submitted successfully! The restaurant will contact you if confirmation is needed.";
                    // Redirect to the restaurant's profile page after successful submission
                    return RedirectToAction("Details", "Restaurant", new { id = model.RestaurantID });
                }
                else // Handle API errors
                {
                    // Handle API errors
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(); // Example DTO
                    _logger.LogError("API call failed: Could not create reservation. Status: {StatusCode}, Reason: {ApiError}",
                        response.StatusCode, errorResponse?.Message ?? await response.Content.ReadAsStringAsync()); // Log full content if DTO fails
                    // Add error message to display to the user
                    ModelState.AddModelError(string.Empty, errorResponse?.Message ?? "Error submitting reservation. Please try again.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error: Could not create reservation for Restaurant {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError(string.Empty, "Error submitting reservation due to a connection issue. Please try again later.");
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "Unexpected error creating reservation for Restaurant {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while submitting the reservation.");
            }
            // --- End API Call ---

            // If API call failed or other error occurred, return the view with the model and errors
            // ** Repopulate RestaurantName again ** (See comment in validation section about improving this)
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Define and use appropriate DTO if API returns something different
                var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>($"api/restaurants/{model.RestaurantID}");
                model.RestaurantName = restaurant?.Name ?? "Restaurant";
            }
            catch { model.RestaurantName = "Restaurant"; }

            return View(model);
        }

        // POST: /Reservation/CreateAjax
        // AJAX endpoint for submitting the reservation request without page refresh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(ReservationViewModel model)
        {
            try 
            {
                // Dictionary to hold validation errors
                var errors = new Dictionary<string, string>();

                // --- Manual Validation (supplements Model Annotations) ---
                if (model.ReservationDateTime <= DateTime.Now)
                {
                    errors.Add(nameof(model.ReservationDateTime), "Reservation must be for a future date and time.");
                    ModelState.AddModelError(nameof(model.ReservationDateTime), "Reservation must be for a future date and time.");
                }

                // Guest-specific required fields (if allowing guest reservations)
                if (!User.Identity.IsAuthenticated)
                {
                    if (string.IsNullOrWhiteSpace(model.ContactName))
                    {
                        errors.Add(nameof(model.ContactName), "Contact Name is required for guest reservations.");
                        ModelState.AddModelError(nameof(model.ContactName), "Contact Name is required for guest reservations.");
                    }
                    if (string.IsNullOrWhiteSpace(model.Phone))
                    {
                        errors.Add(nameof(model.Phone), "Phone Number is required for guest reservations.");
                        ModelState.AddModelError(nameof(model.Phone), "Phone Number is required for guest reservations.");
                    }
                    if (string.IsNullOrWhiteSpace(model.Email))
                    {
                        errors.Add(nameof(model.Email), "Email is required for guest reservations.");
                        ModelState.AddModelError(nameof(model.Email), "Email is required for guest reservations.");
                    }
                }
                // --- End Validation ---

                // Check ModelState validation (catches annotation-based validation)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Reservation CreateAjax POST failed validation for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    // Populate errors dictionary from ModelState if not already added above
                    foreach (var state in ModelState)
                    {
                        if (state.Value.Errors.Count > 0 && !errors.ContainsKey(state.Key))
                        {
                            errors.Add(state.Key, state.Value.Errors.First().ErrorMessage);
                        }
                    }
                    
                    // Return validation errors to the client
                    return Json(new { 
                        success = false, 
                        message = "Please correct the validation errors.",
                        errors = errors
                    });
                }

                // --- Direct Database Save For Testing ---
                // For testing purposes, save directly to the database without using the API
                // In a production scenario, you would want to use the API
                
                // Store the reservation details directly
                string formattedDate = model.ReservationDateTime.ToString("yyyy-MM-dd HH:mm");
                _logger.LogInformation("DEMO MODE: Storing reservation locally. Restaurant: {RestaurantId}, Date: {Date}, Party: {PartySize}, Contact: {ContactName}",
                    model.RestaurantID, formattedDate, model.PartySize, model.ContactName);
                    
                // In a real implementation, you would store in a local database table
                // For demo purposes, we'll simulate success
                
                return Json(new {
                    success = true,
                    message = "Your reservation has been recorded. Note: This is in demo mode without calling the API.",
                    isOfflineMode = true,
                    reservationId = new Random().Next(1000, 9999) // Generate a fake ID for demo
                });
                
                /* Commenting out real API call for now
                // --- Prepare Data Transfer Object (DTO) for API ---
                var reservationDto = new CreateReservationDto
                {
                    RestaurantID = model.RestaurantID,
                    // Set UserID only if authenticated
                    UserID = User.Identity.IsAuthenticated ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)) : (int?)null,
                    ReservationDateTime = model.ReservationDateTime,
                    PartySize = model.PartySize,
                    ContactName = model.ContactName,
                    Phone = model.Phone,
                    Email = model.Email,
                    SpecialRequests = model.SpecialRequests
                    // API will set Status to 'Pending' and handle CreatedDate
                };
                // --- End DTO Preparation ---

                // --- API Call to submit Reservation ---
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api");
                    string apiUrl = "api/reservations"; 
                    
                    _logger.LogDebug("Calling API POST {ApiUrl} to create reservation", apiUrl);
                    
                    // Use timeout for the API call
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout
                    
                    var response = await client.PostAsJsonAsync(apiUrl, reservationDto, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Reservation created successfully via API for Restaurant {RestaurantId}", model.RestaurantID);
                        
                        // Return success response
                        return Json(new { 
                            success = true, 
                            message = "Reservation submitted successfully! The restaurant will contact you if confirmation is needed.",
                            reservationId = await response.Content.ReadFromJsonAsync<int>() // This might be different based on API response
                        });
                    }
                    else // Handle API errors
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        ErrorResponseDto errorResponse = null;
                        
                        try {
                            errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(errorContent);
                        }
                        catch {
                            // If deserializing fails, we'll use the raw content later
                        }
                        
                        _logger.LogError("API call failed: Could not create reservation. Status: {StatusCode}, Reason: {ApiError}",
                            response.StatusCode, errorResponse?.Message ?? errorContent);
                            
                        return Json(new { 
                            success = false, 
                            message = errorResponse?.Message ?? "Error submitting reservation. Please try again."
                        });
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "API call timed out: Reservation request for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    // Save the reservation locally for later processing (offline mode)
                    SaveReservationForLaterProcessing(reservationDto);
                    
                    return Json(new { 
                        success = true, 
                        message = "Your reservation has been saved in offline mode. It will be processed when the system reconnects.",
                        isOfflineMode = true
                    });
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "API connection error: Could not create reservation for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    // Save the reservation locally for later processing (offline mode)
                    SaveReservationForLaterProcessing(reservationDto);
                    
                    return Json(new { 
                        success = true, 
                        message = "Your reservation has been saved in offline mode. It will be processed when the system reconnects.",
                        isOfflineMode = true
                    });
                }
                catch (Exception ex) // Catch unexpected errors
                {
                    _logger.LogError(ex, "Unexpected error creating reservation for Restaurant {RestaurantId}", model.RestaurantID);
                    return Json(new { 
                        success = false, 
                        message = "An unexpected error occurred while submitting the reservation."
                    });
                }
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in CreateAjax method for restaurant {RestaurantId}", model.RestaurantID);
                return Json(new { 
                    success = false, 
                    message = "An unexpected system error occurred. Please try again or contact support."
                });
            }
        }

        // Helper method to save reservation data for later processing (offline mode)
        private void SaveReservationForLaterProcessing(CreateReservationDto reservation)
        {
            try
            {
                // For now, just log the reservation details
                _logger.LogInformation("OFFLINE MODE: Saved reservation for Restaurant {RestaurantId}, Party Size: {PartySize}, Date/Time: {DateTime}, Contact: {ContactName}",
                    reservation.RestaurantID, reservation.PartySize, reservation.ReservationDateTime, reservation.ContactName);
                
                // In a real implementation, you'd store this in a local file, database, or browser storage
                // Example: Save to TempData, Session, or a local file
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save offline reservation data");
            }
        }

        // GET: /Reservation/GetInitialModel?restaurantId=123
        // Returns a partial view with a pre-filled reservation model for the modal
        [HttpGet]
        public IActionResult GetInitialModel(int restaurantId)
        {
            try
            {
                var viewModel = new ReservationViewModel
                {
                    RestaurantID = restaurantId,
                    ReservationDateTime = DateTime.Today.AddDays(1).AddHours(19),
                    PartySize = 2
                };

                // Set restaurant name directly (demo mode)
                viewModel.RestaurantName = $"Demo Restaurant #{restaurantId}";
                ViewBag.DemoMode = "The application is running in demo mode.";

                // Pre-fill user info if logged in
                if (User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        viewModel.UserID = userId;
                    }
                    viewModel.ContactName = User.Identity.Name;
                    viewModel.Email = User.FindFirstValue(ClaimTypes.Email);
                }

                return PartialView("_ReservationModal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating initial reservation model for restaurant {RestaurantId}", restaurantId);
                return StatusCode(500, "An error occurred while preparing the reservation form.");
            }
        }
    }
}