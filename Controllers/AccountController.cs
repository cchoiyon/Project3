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
using System.Text.Encodings.Web; // For Url.Encode if needed in email link generation (now likely done by API)
using Microsoft.AspNetCore.Http.HttpResults;
using Project3.Models.Domain;
using Project3.Utilities;
using System.Xml.Linq;


// Note: Removed using statements for System.Data, System.Data.SqlClient, Project3.Utilities

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
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }
            ViewBag.SuccessMessage = TempData["Message"];
            return View(); // Return Views/Account/Login.cshtml
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = "api/AccountApi/login"; // Corrected URL

                _logger.LogDebug("Calling API POST {ApiUrl} for user {Username}", apiUrl, model.Username);
                var response = await client.PostAsJsonAsync(apiUrl, model);

                if (response.IsSuccessStatusCode) // 2xx Status Code (e.g., 200 OK)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                    if (loginResponse == null || !loginResponse.IsAuthenticated)
                    {
                        _logger.LogWarning("API login successful (200 OK) but returned invalid data (IsAuthenticated=false or null) for user: {Username}", model.Username);
                        ModelState.AddModelError(string.Empty, "Login failed. Authentication service returned unexpected data.");
                        return View(model);
                    }

                    // --- MODIFICATION FOR TESTING: Bypassed Verification Check ---
                    /*
                    if (!loginResponse.IsVerified)
                    {
                        _logger.LogWarning("Login attempt failed for unverified user: {Username}", model.Username);
                        ModelState.AddModelError(string.Empty, "Your account has not been verified. Please check your email.");
                        return View(model);
                    }
                    */
                    _logger.LogWarning("TESTING: Email verification check bypassed for user {Username}", model.Username);
                    // --- END MODIFICATION ---

                    var claims = new List<Claim> { /* ... claims setup ... */ };
                    claims.Add(new Claim(ClaimTypes.Name, loginResponse.Username));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()));
                    claims.Add(new Claim(ClaimTypes.Role, loginResponse.Role));
                    claims.Add(new Claim(ClaimTypes.Email, loginResponse.Email ?? ""));

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in successfully via API and signed in.", model.Username);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) { return Redirect(returnUrl); }
                    else { return RedirectToDashboard(loginResponse.Role); }
                }
                else // Handle non-success status codes (4xx, 5xx)
                {
                    string apiError = $"API Error: Status Code {response.StatusCode}";
                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
                        apiError = errorResponse?.Message ?? apiError;
                    }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from API. Status Code: {StatusCode}", response.StatusCode); }

                    _logger.LogWarning("API login failed for user {Username}. Status: {StatusCode}. Reason: {ApiError}", model.Username, response.StatusCode, apiError);
                    ModelState.AddModelError(string.Empty, apiError);
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
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }
            return View(); // Return Views/Account/Register.cshtml
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }
            if (model.Password != model.ConfirmPassword) ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");
            if (!ModelState.IsValid) { return View(model); }

            try
            {
                string hashedAnswer1 = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer1);
                string hashedAnswer2 = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer2);
                string hashedAnswer3 = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer3);

                var registrationData = new Project3.Controllers.API.AccountApiController.RegisterRequestDto(
                    model.Username, model.Email, model.Password, model.UserRole, model.FirstName, model.LastName,
                    model.SecurityQuestion1, hashedAnswer1, model.SecurityQuestion2, hashedAnswer2, model.SecurityQuestion3, hashedAnswer3
                );

                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = "api/AccountApi/register"; // Corrected URL

                _logger.LogDebug("Calling API POST {ApiUrl} for user registration {Username}", apiUrl, model.Username);
                var response = await client.PostAsJsonAsync(apiUrl, registrationData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Registration submitted successfully via API for user {Username}.", model.Username);
                    TempData["Message"] = "Registration successful! Please check your email to verify your account (if applicable).";
                    return RedirectToAction("Login");
                }
                else // Handle API errors
                {
                    string apiError = $"API Registration Error: Status Code {response.StatusCode}";
                    try { var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(); apiError = errorResponse?.Message ?? apiError; }
                    catch (Exception readEx) { _logger.LogWarning(readEx, "Could not parse error response from Registration API. Status Code: {StatusCode}", response.StatusCode); }
                    _logger.LogWarning("API registration failed for user {Username}. Status: {StatusCode}. Reason: {ApiError}", model.Username, response.StatusCode, apiError);
                    ModelState.AddModelError(string.Empty, apiError);
                    return View(model);
                }
            }
            catch (HttpRequestException ex) { /* ... Log and handle ... */ ModelState.AddModelError(string.Empty, "Connection error during registration."); return View(model); }
            catch (Exception ex) { /* ... Log and handle ... */ ModelState.AddModelError(string.Empty, "Unexpected error during registration."); return View(model); }
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
            { /* ... Handle missing code ... */ ViewBag.Message = "Invalid link."; return View(); }

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = "api/AccountApi/confirm-email"; // Corrected URL
                var verificationData = new { VerificationToken = code };

                _logger.LogDebug("Calling API POST {ApiUrl} to confirm email", apiUrl);
                var response = await client.PostAsJsonAsync(apiUrl, verificationData);

                if (response.IsSuccessStatusCode)
                { /* ... Handle success ... */ TempData["Message"] = "Email confirmed. You can log in."; return RedirectToAction("Login"); }
                else
                { /* ... Handle API error ... */
                    string apiError = "Email confirmation failed."; try { var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(); apiError = errorResponse?.Message ?? apiError; } catch { }
                    ViewBag.Message = apiError; return View();
                }
            }
            catch (HttpRequestException ex) { /* ... Log and handle ... */ ViewBag.Message = "Connection error."; return View(); }
            catch (Exception ex) { /* ... Log and handle ... */ ViewBag.Message = "Unexpected error."; return View(); }
        }


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {Username} logged out.", User.Identity?.Name ?? "Unknown");
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        { return View(); }


        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        { return View(); }

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
                string apiUrl = "api/AccountApi/forgot-password"; // Corrected URL
                var requestData = new Project3.Controllers.API.AccountApiController.ForgotPasswordRequestDto(model.EmailOrUsername);
                var response = await client.PostAsJsonAsync(apiUrl, requestData);
                if (!response.IsSuccessStatusCode) { /* Log API error */ }
            }
            catch (Exception ex) { /* Log error */ }
            ViewBag.Message = "If an account matching your submission exists, instructions have been sent.";
            return View("ForgotPasswordConfirmation");
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
            { return RedirectToAction(nameof(Login), new { message = "Invalid password reset link." }); }
            var model = new ResetPasswordModel { UserId = userId, Token = token };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.NewPassword != model.ConfirmPassword)
            { ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match."); return View(model); }

            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = "api/AccountApi/reset-password"; // Corrected URL
                var requestData = new Project3.Controllers.API.AccountApiController.ResetPasswordRequestDto(model.UserId, model.Token, model.NewPassword);
                var response = await client.PostAsJsonAsync(apiUrl, requestData);

                if (response.IsSuccessStatusCode) { return RedirectToAction("ResetPasswordConfirmation"); }
                else
                { /* Handle API error */
                    string apiError = "Password reset failed."; try { var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(); apiError = errorResponse?.Message ?? apiError; } catch { }
                    ModelState.AddModelError("", apiError); return View(model);
                }
            }
            catch (HttpRequestException ex) { /* Log and handle */ ModelState.AddModelError("", "Connection error."); return View(model); }
            catch (Exception ex) { /* Log and handle */ ModelState.AddModelError("", "Unexpected error."); return View(model); }
        }

        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        { return View(); }

        // *** ADDED ForgotUsername Actions (Stubs) ***
        // GET: /Account/ForgotUsername
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotUsername()
        {
            // TODO: Create a corresponding View: Views/Account/ForgotUsername.cshtml
            return View();
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
                // TODO: Define ForgotUsernameRequestDto if needed for API call
                // var requestData = new ForgotUsernameRequestDto(model.Email); // Example

                var client = _httpClientFactory.CreateClient("Project3Api");
                // TODO: Implement the corresponding API endpoint: POST api/AccountApi/forgot-username
                string apiUrl = "api/AccountApi/forgot-username"; // Define correct API route

                // TODO: Make the API call
                // var response = await client.PostAsJsonAsync(apiUrl, requestData);

                // TODO: Handle API response (success or failure)
                // Regardless of whether the email exists, show a confirmation message to prevent enumeration
                // if (!response.IsSuccessStatusCode) { /* Log API error */ }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Forgot Username request for {Email}", model.Email);
                // Don't expose specific errors to the user
            }

            // Always show a confirmation view/message
            ViewBag.Message = "If an account matching your email exists, your username has been sent.";
            // TODO: Create a corresponding View: Views/Account/ForgotUsernameConfirmation.cshtml
            return View("ForgotUsernameConfirmation"); // Or redirect to Login with TempData message
        }
        // *** END ADDED ForgotUsername Actions ***


        // --- Helper Methods ---
        private IActionResult RedirectToDashboard(string userRole)
        {
            if ("reviewer".Equals(userRole, StringComparison.OrdinalIgnoreCase)) { return RedirectToAction("Index", "ReviewerHome"); }
            else if ("restaurantRep".Equals(userRole, StringComparison.OrdinalIgnoreCase)) { return RedirectToAction("Index", "RestaurantRepHome"); }
            else { _logger.LogWarning("Unknown role '{Role}'. Defaulting to Home.", userRole); return RedirectToAction("Index", "Home"); }
        }

        private string GetUserRole()
        { return User.FindFirstValue(ClaimTypes.Role) ?? "Guest"; }

        // DTO Definitions (Consider moving DTOs to a shared location)
        private record LoginResponseDto(bool IsAuthenticated, bool IsVerified, int UserId, string Username, string Email, string Role);
        private record ErrorResponseDto(string Message);

    }
}
