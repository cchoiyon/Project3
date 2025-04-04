using Microsoft.AspNetCore.Mvc;
using Project3.Models.InputModels; // Assuming LoginModel, RegisterModel, etc. are here
using Microsoft.AspNetCore.Authentication; // Needed for HttpContext.SignInAsync/SignOutAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for CookieAuthenticationDefaults
using System.Security.Claims; // Needed for ClaimsPrincipal, ClaimTypes, Claim
using System.Threading.Tasks; // Needed for async Task
using System.Collections.Generic; // Needed for List<Claim>
using Microsoft.AspNetCore.Authorization; // Needed for [AllowAnonymous], [Authorize]
using Microsoft.Extensions.Logging; // Needed for ILogger
using System.Net.Http; // Needed for IHttpClientFactory
using System.Net.Http.Json; // Needed for PostAsJsonAsync etc.
using Project3.Models.DTOs; // Needed for LoginResponseDto, ErrorResponseDto etc.
using System; // Needed for Exception, StringComparison etc.

// Define the namespace for your controllers
// Make sure this matches your project structure if it's different
// namespace Project3.Controllers
// {

/// <summary>
/// Controller responsible for handling user account actions like
/// Login, Logout, Registration, Password Reset, etc.
/// </summary>
public class AccountController : Controller
{
    // Dependency injected services
    private readonly ILogger<AccountController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Constructor to initialize the controller with required services.
    /// </summary>
    /// <param name="logger">Logger instance for logging information and errors.</param>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances to call the API.</param>
    public AccountController(
        ILogger<AccountController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // --- Login (GET) ---
    /// <summary>
    /// Displays the login page.
    /// If the user is already authenticated, redirects them to their dashboard.
    /// </summary>
    /// <param name="returnUrl">The URL to return to after successful login.</param>
    /// <returns>The Login view or a redirect result.</returns>
    [HttpGet]
    [AllowAnonymous] // Allows access to this page even if not logged in
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        // If user is already logged in, redirect them away from the login page
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToDashboard(GetUserRole());
        }
        // Pass any success message from TempData (e.g., after logout or email confirmation)
        ViewBag.SuccessMessage = TempData["Message"];
        return View(); // Renders Views/Account/Login.cshtml
    }


    // --- Login (POST) ---
    /// <summary>
    /// Handles the login form submission.
    /// Calls the backend API to validate credentials and signs the user in upon success.
    /// </summary>
    /// <param name="model">The login form data (username, password, remember me).</param>
    /// <param name="returnUrl">The URL to return to after successful login.</param>
    /// <returns>Redirects to the dashboard or return URL on success, otherwise redisplays the login view with errors.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken] // Prevents Cross-Site Request Forgery attacks
    public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        // Prevent already logged-in users from attempting to log in again via POST
        if (User.Identity != null && User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }

        // If model validation fails (e.g., required fields missing), redisplay the form
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Create an HttpClient instance configured to call your API
            var client = _httpClientFactory.CreateClient("Project3Api");
            string apiUrl = "api/AccountApi/login"; // Ensure this matches your API endpoint

            // Send the login model data to the API
            var response = await client.PostAsJsonAsync(apiUrl, model);

            // Check if the API call was successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response data from the API
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                _logger.LogInformation("API Login Response for User: {Username}, Role: {Role}", loginResponse?.Username, loginResponse?.Role);

                // Basic check on the API response
                if (loginResponse == null || !loginResponse.IsAuthenticated)
                {
                    ModelState.AddModelError(string.Empty, "Login failed. Invalid response from server.");
                    return View(model);
                }

                // TODO: Implement email verification check if needed
                // if (!loginResponse.IsVerified) { ... return View with message ... }
                _logger.LogWarning("TESTING: Email verification check bypassed for user {Username}", model.Username);


                // --- Standardize Role Casing Before Creating Claim ---
                // Ensures consistency when checking roles later (e.g., User.IsInRole)
                string roleClaimValue = string.Empty;
                if (!string.IsNullOrEmpty(loginResponse.Role))
                {
                    if ("Reviewer".Equals(loginResponse.Role, StringComparison.OrdinalIgnoreCase))
                    {
                        roleClaimValue = "Reviewer"; // Use standard casing
                    }
                    else if ("RestaurantRep".Equals(loginResponse.Role, StringComparison.OrdinalIgnoreCase))
                    {
                        roleClaimValue = "RestaurantRep"; // Use standard casing
                    }
                    else
                    {
                        _logger.LogWarning("Unrecognized role '{Role}' received from API for user {Username}. Storing as received.", loginResponse.Role, loginResponse.Username);
                        roleClaimValue = loginResponse.Role; // Store unrecognized roles as-is
                    }
                }
                else
                {
                    _logger.LogWarning("No role received from API for user {Username}.", loginResponse.Username);
                }


                // Create the claims for the user's identity cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()), // User's unique ID
                    new Claim(ClaimTypes.Name, loginResponse.Username), // User's username
                    new Claim(ClaimTypes.Role, roleClaimValue), // User's role (standardized)
                    new Claim(ClaimTypes.Email, loginResponse.Email ?? string.Empty) // User's email
                    // Add other claims if needed
                };

                // Create the identity and principal for cookie authentication
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    // Allow the login session to persist across browser closures if "Remember Me" is checked
                    IsPersistent = model.RememberMe,
                    // You can set other properties like ExpiresUtc if needed
                };

                // Sign the user in using cookie authentication
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Username} logged in successfully via API and signed in with standardized role '{RoleClaim}'.", model.Username, roleClaimValue);

                // Redirect the user to their appropriate dashboard based on role
                _logger.LogInformation("Redirecting user {Username} with role {Role} to dashboard.", loginResponse.Username, roleClaimValue);
                // Use the helper method to determine the correct dashboard
                return RedirectToDashboard(roleClaimValue);
            }
            else // Handle unsuccessful API responses (e.g., 400 Bad Request, 401 Unauthorized)
            {
                // Try to read an error message from the API response body if available
                string apiError = "Invalid username or password."; // Default message
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                    if (!string.IsNullOrWhiteSpace(errorResponse?.Message))
                    {
                        apiError = errorResponse.Message;
                    }
                }
                catch (Exception readEx)
                {
                    _logger.LogWarning(readEx, "Could not read error response body from API during failed login for {Username}.", model.Username);
                }

                _logger.LogWarning("API login failed for user {Username}. Status Code: {StatusCode}. Reason: {ReasonPhrase}", model.Username, response.StatusCode, response.ReasonPhrase);
                ModelState.AddModelError(string.Empty, apiError);
                return View(model); // Redisplay login form with error
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP request error during login API call for user: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Could not connect to the login service. Please try again later.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login process for user: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
            return View(model);
        }
    }

    // --- Logout Action ---
    /// <summary>
    /// Logs the current user out by removing their authentication cookie.
    /// </summary>
    /// <returns>Redirects to the Login page.</returns>
    [HttpPost] // Should be POST to prevent accidental logout via GET requests
    [ValidateAntiForgeryToken]
    // [Authorize] // Optional: Ensure only logged-in users can attempt to log out
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name ?? "Unknown";
        // Clear the existing external cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {Username} logged out.", username);
        TempData["Message"] = "You have been logged out successfully."; // Set success message for Login page
        return RedirectToAction("Login"); // Redirect to the login page
    }

    // --- Other Actions (Register, ForgotPassword, ResetPassword, etc.) ---

    /// <summary>
    /// Displays the user registration page.
    /// </summary>
    /// <returns>The Register view.</returns>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        // You might initialize a RegisterModel here if needed by the view
        // var model = new RegisterModel();
        return View(); // Renders Views/Account/Register.cshtml
    }

    /// <summary>
    /// Displays the forgot password page (where user enters email/username).
    /// </summary>
    /// <returns>The ForgotPassword view.</returns>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(); // Renders Views/Account/ForgotPassword.cshtml
    }

    /// <summary>
    /// Displays the forgot username page (where user enters email).
    /// </summary>
    /// <returns>The ForgotUsername view.</returns>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotUsername()
    {
        return View(); // Renders Views/Account/ForgotUsername.cshtml
    }

    // TODO: Implement the [HttpPost] actions for Register, ForgotPassword, ForgotUsername
    // These methods will typically call your backend API. Example structure:
    /*
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        // Call API to register user...
        // Handle API response (success/failure)...
        // Redirect on success (e.g., to RegistrationConfirmationSent or Login)
        // Return View(model) with errors on failure
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
    {
         if (!ModelState.IsValid)
        {
            return View(model);
        }
        // Call API to initiate password reset...
        // Redirect to ForgotPasswordConfirmation on success
        // Return View(model) with errors on failure
    }
    */

    // TODO: Implement other necessary actions based on your workflow
    // (ConfirmEmail, ResetPassword GET/POST, Confirmation pages, AccessDenied etc.)
    // Example:
    /*
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
    */


    // --- Helper Methods ---

    /// <summary>
    /// Determines the correct dashboard action based on the user's role.
    /// </summary>
    /// <param name="userRole">The role claim value of the user.</param>
    /// <returns>An IActionResult redirecting to the appropriate controller/action.</returns>
    private IActionResult RedirectToDashboard(string userRole)
    {
        // Use case-insensitive comparison for robustness, although the claim should be standardized now.
        if ("Reviewer".Equals(userRole, StringComparison.OrdinalIgnoreCase))
        {
            // Redirect Reviewers to ReviewerHomeController's Index action
            return RedirectToAction("Index", "ReviewerHome");
        }
        else if ("RestaurantRep".Equals(userRole, StringComparison.OrdinalIgnoreCase))
        {
            // Redirect Restaurant Reps to RestaurantRepHomeController's Index action
            return RedirectToAction("Index", "RestaurantRepHome");
        }
        else
        {
            // Fallback for unknown roles or if role is missing
            _logger.LogWarning("RedirectToDashboard called with unknown or empty role '{Role}'. Redirecting to Login.", userRole ?? "[null]");
            // Redirect back to Login page or a generic landing page if preferred
            return RedirectToAction("Login", "Account");
        }
    }

    /// <summary>
    /// Retrieves the user's role claim from the current ClaimsPrincipal.
    /// </summary>
    /// <returns>The user's role string, or null if not found.</returns>
    private string GetUserRole()
    {
        // Find the first claim of type Role
        return User.FindFirstValue(ClaimTypes.Role);
    }

    // --- DTO Definitions ---
    // These should ideally live in your Models/DTOs folder, but are included here
    // for completeness based on the original code structure provided.
    // Ensure these record definitions match the JSON structure returned by your API.

    /// <summary>
    /// Represents the data returned by the login API endpoint on success.
    /// </summary>
    private record LoginResponseDto(bool IsAuthenticated, bool IsVerified, int UserId, string Username, string Email, string Role);

    /// <summary>
    /// Represents a generic error message returned by the API.
    /// </summary>
    private record ErrorResponseDto(string Message);

    // Add other DTO records used by this controller if needed, e.g.:
    // private record ForgotPasswordRequestDto(string EmailOrUsername);
    // private record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);

} // End of AccountController class

// } // End of namespace if you uncommented it at the top
