using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Diagnostics; // For ErrorViewModel (if needed, maybe only in HomeController)
using Project3.Models.Configuration; // Using organized namespace
using Project3.Models.InputModels; // Using organized namespace
// using Project3.Models.ViewModels; // Add if needed
using Project3.Models.DTOs; // Add using for your API DTOs
using System.Net.Http; // For HttpClient
using System.Net.Http.Json; // For GetFromJsonAsync, PostAsJsonAsync etc.
using System.Net; // For HttpStatusCode
using System.Text.Encodings.Web; // For Url.Encode if needed
using Microsoft.AspNetCore.Http.HttpResults;
using Project3.Models.Domain;
using Project3.Utilities;
using System.Xml.Linq; // Note: This seems unused, consider removing if not needed.
using BCrypt.Net; // Assuming BCrypt is used for hashing in Register

// Note: Removed using statements for System.Data, System.Data.SqlClient, Project3.Utilities if not used directly here

namespace Project3.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IHttpClientFactory _httpClientFactory; // Inject HttpClientFactory

        // Constructor updated
        public AccountController(
            ILogger<AccountController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            // If user is already logged in, redirect them immediately
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard(GetUserRole());
            }
            ViewBag.SuccessMessage = TempData["Message"]; // Show messages from RedirectToAction (e.g., after registration)
            return View(); // Return Views/Account/Login.cshtml
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity != null && User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // Ensure API route matches your AccountApiController's route/action name
                string apiUrl = "api/AccountApi/login";

                _logger.LogDebug("Calling API POST {ApiUrl} for user {Username}", apiUrl, model.Username);
                // Send the LoginModel directly if your API accepts it
                var response = await client.PostAsJsonAsync(apiUrl, model);

                if (response.IsSuccessStatusCode) // 2xx Status Code (e.g., 200 OK)
                {
                    // Expecting LoginResponseDto defined below or in your DTOs folder
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                    if (loginResponse == null || !loginResponse.IsAuthenticated)
                    {
                        _logger.LogWarning("API login successful (200 OK) but returned invalid data (IsAuthenticated=false or null) for user: {Username}", model.Username);
                        ModelState.AddModelError(string.Empty, "Login failed. Authentication service returned unexpected data.");
                        return View(model);
                    }

                    // --- MODIFICATION FOR TESTING: Bypassed Verification Check ---
                    /* // Re-enable this check when email verification is working
                    if (!loginResponse.IsVerified)
                    {
                        _logger.LogWarning("Login attempt failed for unverified user: {Username}", model.Username);
                        ModelState.AddModelError(string.Empty, "Your account has not been verified. Please check your email.");
                        return View(model);
                    }
                    */
                    _logger.LogWarning("TESTING: Email verification check bypassed for user {Username}", model.Username);
                    // --- END MODIFICATION ---

                    // --- Create Claims ---
                    var claims = new List<Claim>
                    {
                        // Use NameIdentifier for the user's unique ID
                        new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()),
                        // Use Name for the username (often displayed)
                        new Claim(ClaimTypes.Name, loginResponse.Username),
                         // Use Role for authorization checks
                        new Claim(ClaimTypes.Role, loginResponse.Role ?? string.Empty), // Handle potential null role
                        // Include Email if needed elsewhere
                        new Claim(ClaimTypes.Email, loginResponse.Email ?? string.Empty) // Handle potential null email
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        // Set cookie persistence based on "Remember Me" checkbox
                        IsPersistent = model.RememberMe
                        // You can set ExpiresUtc here too if needed
                    };

                    // --- Sign in the user (Creates the authentication cookie) ---
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in successfully via API and signed in.", model.Username);

                    // --- *** CORRECTED REDIRECTION LOGIC *** ---
                    // Always prioritize redirecting to the specific dashboard based on role after login.
                    _logger.LogInformation("Redirecting user {Username} with role {Role} to dashboard.", loginResponse.Username, loginResponse.Role);
                    return RedirectToDashboard(loginResponse.Role);

                    // --- Old Logic (commented out) ---
                    // if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    // {
                    //     _logger.LogDebug("Redirecting user {Username} to returnUrl: {ReturnUrl}", loginResponse.Username, returnUrl);
                    //     return Redirect(returnUrl); // Changed to Redirect from LocalRedirect based on prior code
                    // }
                    // else
                    // {
                    //     _logger.LogDebug("No valid returnUrl, redirecting user {Username} with role {Role} to dashboard.", loginResponse.Username, loginResponse.Role);
                    //     return RedirectToDashboard(loginResponse.Role);
                    // }
                    // --- End Old Logic ---

                }
                else // Handle non-success status codes (4xx, 5xx) from API
                {
                    string apiError = $"API Error: Status Code {response.StatusCode}";
                    try
                    {
                        // Assuming ErrorResponseDto is defined below or in your DTOs folder
                        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                        apiError = errorResponse?.Message ?? apiError;
                    }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from API. Status Code: {StatusCode}", response.StatusCode); }

                    _logger.LogWarning("API login failed for user {Username}. Status: {StatusCode}. Reason: {ApiError}", model.Username, response.StatusCode, apiError);
                    // Use a generic message unless the API error is specifically user-friendly
                    ModelState.AddModelError(string.Empty, "Invalid username or password."); // More user-friendly than showing raw API error
                    return View(model);
                }
            }
            catch (HttpRequestException ex) // Network/connection error
            {
                _logger.LogError(ex, "API connection error during login for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred connecting to the login service. Please try again later.");
                return View(model);
            }
            catch (Exception ex) // Catch other unexpected errors
            {
                _logger.LogError(ex, "Unexpected error during login process for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
                return View(model);
            }
        }


        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }
            return View(); // Return Views/Account/Register.cshtml
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }

            // Manual check for password match (alternative to Compare attribute)
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
            }

            if (!ModelState.IsValid) { return View(model); }

            try
            {
                // Hash the security answers before sending to API
                string hashedAnswer1 = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer1);
                string hashedAnswer2 = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer2);
                string hashedAnswer3 = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer3);

                // Prepare data for the API call - Using the nested DTO definition from your code
                var registrationData = new Project3.Controllers.API.AccountApiController.RegisterRequestDto(
                    model.Username, model.Email, model.Password, model.UserRole, model.FirstName, model.LastName,
                    model.SecurityQuestion1, hashedAnswer1, model.SecurityQuestion2, hashedAnswer2, model.SecurityQuestion3, hashedAnswer3
                );

                var client = _httpClientFactory.CreateClient("Project3Api");
                // Ensure API route matches your AccountApiController's route/action name
                string apiUrl = "api/AccountApi/register";

                _logger.LogDebug("Calling API POST {ApiUrl} for user registration {Username}", apiUrl, model.Username);
                var response = await client.PostAsJsonAsync(apiUrl, registrationData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Registration submitted successfully via API for user {Username}.", model.Username);
                    // Use TempData to show a message on the Login page after redirect
                    TempData["Message"] = "Registration successful! Please check your email to verify your account (verification currently bypassed for testing).";
                    return RedirectToAction("Login");
                }
                else // Handle API errors
                {
                    string apiError = $"API Registration Error: Status Code {response.StatusCode}";
                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                        apiError = errorResponse?.Message ?? apiError;
                    }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from Registration API. Status Code: {StatusCode}", response.StatusCode); }

                    _logger.LogWarning("API registration failed for user {Username}. Status: {StatusCode}. Reason: {ApiError}", model.Username, response.StatusCode, apiError);
                    // Add API error to ModelState to display on the registration form
                    ModelState.AddModelError(string.Empty, apiError);
                    return View(model);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error during registration for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Connection error during registration.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration process for user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Unexpected error during registration.");
                return View(model);
            }
        }

        // GET: /Account/RegistrationConfirmationSent
        [AllowAnonymous]
        public IActionResult RegistrationConfirmationSent()
        { return View(); }


        // GET: /Account/ConfirmEmail?code=abcdef
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                ViewBag.Message = "Invalid email confirmation link.";
                return View(); // Need a Views/Account/ConfirmEmail.cshtml
            }

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // Ensure API route matches your AccountApiController's route/action name
                string apiUrl = "api/AccountApi/confirm-email";
                // Assuming API expects a simple object with the token
                var verificationData = new { VerificationToken = code }; // Use anonymous type

                _logger.LogDebug("Calling API POST {ApiUrl} to confirm email", apiUrl);
                var response = await client.PostAsJsonAsync(apiUrl, verificationData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email confirmed successfully via API for token (hidden).");
                    TempData["Message"] = "Email confirmed successfully. You can now log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    string apiError = "Email confirmation failed.";
                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                        apiError = errorResponse?.Message ?? apiError;
                    }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from ConfirmEmail API. Status Code: {StatusCode}", response.StatusCode); }
                    _logger.LogWarning("API email confirmation failed. Status: {StatusCode}. Reason: {ApiError}", response.StatusCode, apiError);
                    ViewBag.Message = apiError;
                    return View(); // Show error on ConfirmEmail.cshtml view
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error during email confirmation.");
                ViewBag.Message = "Connection error confirming email. Please try again later.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during email confirmation.");
                ViewBag.Message = "Unexpected error confirming email.";
                return View();
            }
        }


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown"; // Get username before signing out
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {Username} logged out.", username);
            // Redirect to login page after logout, maybe with a message
            TempData["Message"] = "You have been logged out.";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        [AllowAnonymous] // Allow anyone to see the access denied page
        public IActionResult AccessDenied()
        { return View(); } // Need Views/Account/AccessDenied.cshtml


        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        { return View(); } // Need Views/Account/ForgotPassword.cshtml

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _logger.LogInformation("Forgot Password request received via MVC for: {Identifier}", model.EmailOrUsername);
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // Ensure API route matches your AccountApiController's route/action name
                string apiUrl = "api/AccountApi/forgot-password";
                // Assuming API expects a DTO like this (defined below or elsewhere)
                var requestData = new ForgotPasswordRequestDto(model.EmailOrUsername);
                var response = await client.PostAsJsonAsync(apiUrl, requestData);

                // Log regardless of success/failure to avoid info leakage
                _logger.LogInformation("Forgot password API call completed for {Identifier} with status {StatusCode}", model.EmailOrUsername, response.StatusCode);
                // if (!response.IsSuccessStatusCode) { /* Log API error details if needed */ }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Forgot Password request for {Identifier}", model.EmailOrUsername);
                // Don't expose specific errors
            }
            // Always show the confirmation page/message to prevent account enumeration
            ViewBag.Message = "If an account matching your submission exists, instructions for resetting your password have been sent.";
            return View("ForgotPasswordConfirmation"); // Need Views/Account/ForgotPasswordConfirmation.cshtml
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        { return View(); }


        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                // Don't reveal specific reasons for failure
                TempData["Message"] = "Invalid password reset link.";
                return RedirectToAction(nameof(Login));
            }
            // Pass necessary info to the view model for the form
            var model = new ResetPasswordModel { UserId = userId, Token = token };
            return View(model); // Need Views/Account/ResetPassword.cshtml
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "The new password and confirmation password do not match.");
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                // Ensure API route matches your AccountApiController's route/action name
                string apiUrl = "api/AccountApi/reset-password";
                // Assuming API expects a DTO like this (defined below or elsewhere)
                var requestData = new ResetPasswordRequestDto(model.UserId, model.Token, model.NewPassword);
                var response = await client.PostAsJsonAsync(apiUrl, requestData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password reset successful via API for UserId {UserId}", model.UserId);
                    return RedirectToAction("ResetPasswordConfirmation"); // Need Views/Account/ResetPasswordConfirmation.cshtml
                }
                else
                {
                    string apiError = "Password reset failed.";
                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                        apiError = errorResponse?.Message ?? apiError;
                    }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from ResetPassword API. Status Code: {StatusCode}", response.StatusCode); }
                    _logger.LogWarning("API password reset failed for UserId {UserId}. Status: {StatusCode}. Reason: {ApiError}", model.UserId, response.StatusCode, apiError);
                    ModelState.AddModelError("", apiError); // Show error on the form
                    return View(model);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error during password reset for UserId {UserId}", model.UserId);
                ModelState.AddModelError("", "Connection error during password reset.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset for UserId {UserId}", model.UserId);
                ModelState.AddModelError("", "Unexpected error during password reset.");
                return View(model);
            }
        }

        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        { return View(); }


        // GET: /Account/ForgotUsername
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotUsername()
        {
            return View(); // Need Views/Account/ForgotUsername.cshtml
        }

        // POST: /Account/ForgotUsername
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotUsername(ForgotUsernameModel model) // Assuming ForgotUsernameModel exists
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _logger.LogInformation("Forgot Username request received via MVC for: {Email}", model.Email); // Assuming model has Email

            try
            {
                // Assuming API expects a simple object with Email
                var requestData = new { Email = model.Email }; // Use anonymous type if no specific DTO

                var client = _httpClientFactory.CreateClient("Project3Api");
                // Ensure API route matches your AccountApiController's route/action name
                string apiUrl = "api/AccountApi/forgot-username";

                _logger.LogDebug("Calling API POST {ApiUrl} for forgot username", apiUrl);
                var response = await client.PostAsJsonAsync(apiUrl, requestData);

                _logger.LogInformation("Forgot username API call completed for {Email} with status {StatusCode}", model.Email, response.StatusCode);
                // Log API error details if needed for debugging, but don't expose failure to user
                // if (!response.IsSuccessStatusCode) { /* ... */ }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Forgot Username request for {Email}", model.Email);
                // Don't expose specific errors to the user
            }

            // Always show a confirmation view/message to prevent email enumeration
            ViewBag.Message = "If an account matching your email exists, your username reminder has been sent.";
            return View("ForgotUsernameConfirmation"); // Need Views/Account/ForgotUsernameConfirmation.cshtml
        }

        // GET: /Account/ForgotUsernameConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotUsernameConfirmation()
        { return View(); }


        // --- Helper Methods ---
        // Helper to redirect based on role claim
        private IActionResult RedirectToDashboard(string userRole)
        {
            // Use case-insensitive comparison for robustness
            if ("Reviewer".Equals(userRole, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "ReviewerHome");
            }
            else if ("RestaurantRep".Equals(userRole, StringComparison.OrdinalIgnoreCase))
            {
                // Corrected controller name if needed (e.g., RestaurantRepHome vs RestaurantRepresentativeHome)
                return RedirectToAction("Index", "RestaurantRepHome");
            }
            else
            {
                // Fallback for unknown roles or if role claim is missing/empty
                _logger.LogWarning("RedirectToDashboard called with unknown or empty role '{Role}'. Defaulting to Home.", userRole ?? "[null]");
                return RedirectToAction("Index", "Home");
            }
        }

        // Helper to safely get the user's role claim
        private string GetUserRole()
        {
            // FindFirstValue returns null if the claim doesn't exist
            return User.FindFirstValue(ClaimTypes.Role); // Returns null if no role claim
        }

        // --- DTO Definitions ---
        // Consider moving these to your Models/DTOs folder for better organization
        // Ensure these match the actual DTOs returned by your API
        private record LoginResponseDto(bool IsAuthenticated, bool IsVerified, int UserId, string Username, string Email, string Role);
        private record ErrorResponseDto(string Message);
        private record ForgotPasswordRequestDto(string EmailOrUsername); // Matches API expectation
        private record ResetPasswordRequestDto(string UserId, string Token, string NewPassword); // Matches API expectation

    }
}
