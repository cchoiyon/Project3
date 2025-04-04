using Microsoft.AspNetCore.Mvc;
using Project3.Models.InputModels; // Assuming LoginModel is here
using Microsoft.AspNetCore.Authentication; // Needed for HttpContext.SignInAsync/SignOutAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for CookieAuthenticationDefaults
using System.Security.Claims; // Needed for ClaimsPrincipal, ClaimTypes, Claim
using System.Threading.Tasks; // Needed for async Task
using System.Collections.Generic; // Needed for List<Claim>
using Microsoft.AspNetCore.Authorization; // Needed for [AllowAnonymous]
using Microsoft.Extensions.Logging; // Needed for ILogger
using System.Net.Http; // Needed for IHttpClientFactory
using System.Net.Http.Json; // Needed for PostAsJsonAsync etc.
using Project3.Models.DTOs; // Needed for LoginResponseDto, ErrorResponseDto
using System; // Needed for Exception, StringComparison etc.

// Add other necessary using statements

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(
        ILogger<AccountController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // --- Login (GET) ---
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToDashboard(GetUserRole());
        }
        ViewBag.SuccessMessage = TempData["Message"];
        return View();
    }


    // --- Login (POST) ---
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
            string apiUrl = "api/AccountApi/login";
            var response = await client.PostAsJsonAsync(apiUrl, model);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                _logger.LogWarning("DEBUG: API Login Response - User: {Username}, Role: {Role}", loginResponse?.Username, loginResponse?.Role);

                if (loginResponse == null || !loginResponse.IsAuthenticated)
                {
                    ModelState.AddModelError(string.Empty, "Login failed. Invalid response from server.");
                    return View(model);
                }

                _logger.LogWarning("TESTING: Email verification check bypassed for user {Username}", model.Username);


                // --- FIX: Standardize Role Casing Before Creating Claim ---
                string roleClaimValue = string.Empty; // Default to empty
                if (!string.IsNullOrEmpty(loginResponse.Role))
                {
                    // Compare case-insensitively, but store with standard casing
                    if ("Reviewer".Equals(loginResponse.Role, StringComparison.OrdinalIgnoreCase))
                    {
                        roleClaimValue = "Reviewer"; // Use standard casing
                    }
                    else if ("RestaurantRep".Equals(loginResponse.Role, StringComparison.OrdinalIgnoreCase))
                    {
                        roleClaimValue = "RestaurantRep"; // Use standard casing
                    }
                    // Add other roles here if needed, ensuring consistent casing
                    else
                    {
                        _logger.LogWarning("Unrecognized role '{Role}' received from API for user {Username}. Storing as received.", loginResponse.Role, loginResponse.Username);
                        roleClaimValue = loginResponse.Role; // Store unrecognized roles as-is (or handle differently)
                    }
                }
                else
                {
                    _logger.LogWarning("No role received from API for user {Username}.", loginResponse.Username);
                }


                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()),
                    new Claim(ClaimTypes.Name, loginResponse.Username),
                    // Use the standardized roleClaimValue here
                    new Claim(ClaimTypes.Role, roleClaimValue),
                    new Claim(ClaimTypes.Email, loginResponse.Email ?? string.Empty)
                };
                // --- End Role Casing Fix ---


                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Username} logged in successfully via API and signed in with standardized role '{RoleClaim}'.", model.Username, roleClaimValue);

                // --- Redirection ---
                // Pass the standardized role to the helper for redirection
                _logger.LogInformation("Redirecting user {Username} with role {Role} to dashboard.", loginResponse.Username, roleClaimValue);
                return RedirectToDashboard(roleClaimValue);
            }
            else
            {
                // ... (Handle API error response) ...
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            // ... (Handle exceptions) ...
            _logger.LogError(ex, "Unexpected error during login process for user: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
            return View(model);
        }
    }

    // --- Logout Action ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name ?? "Unknown";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {Username} logged out.", username);
        TempData["Message"] = "You have been logged out.";
        return RedirectToAction("Login");
    }

    // --- Other Actions (Register, ForgotPassword, ResetPassword, etc.) ---
    // (Code for other actions as previously provided) ...
    // [HttpGet] [AllowAnonymous] public IActionResult Register()...
    // [HttpPost] [AllowAnonymous] [ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterModel model)...
    // [AllowAnonymous] public IActionResult RegistrationConfirmationSent()...
    // [AllowAnonymous] public async Task<IActionResult> ConfirmEmail(string code)...
    // [AllowAnonymous] public IActionResult AccessDenied()...
    // [HttpGet] [AllowAnonymous] public IActionResult ForgotPassword()...
    // [HttpPost] [AllowAnonymous] [ValidateAntiForgeryToken] public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)...
    // [HttpGet] [AllowAnonymous] public IActionResult ForgotPasswordConfirmation()...
    // [HttpGet] [AllowAnonymous] public IActionResult ResetPassword(string userId, string token)...
    // [HttpPost] [AllowAnonymous] [ValidateAntiForgeryToken] public async Task<IActionResult> ResetPassword(ResetPasswordModel model)...
    // [HttpGet] [AllowAnonymous] public IActionResult ResetPasswordConfirmation()...
    // [HttpGet] [AllowAnonymous] public IActionResult ForgotUsername()...
    // [HttpPost] [AllowAnonymous] [ValidateAntiForgeryToken] public async Task<IActionResult> ForgotUsername(ForgotUsernameModel model)...
    // [HttpGet] [AllowAnonymous] public IActionResult ForgotUsernameConfirmation()...


    // --- Helper Methods ---
    private IActionResult RedirectToDashboard(string userRole)
    {
        // This check can remain case-insensitive for robustness, but the claim itself should now have standard casing.
        if ("Reviewer".Equals(userRole, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "ReviewerHome");
        }
        else if ("RestaurantRep".Equals(userRole, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "RestaurantRepHome");
        }
        else
        {
            _logger.LogWarning("RedirectToDashboard called with unknown or empty role '{Role}'. Redirecting to Login.", userRole ?? "[null]");
            return RedirectToAction("Login", "Account"); // Fallback to Login if role is unknown
        }
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

    // --- DTO Definitions ---
    // Ensure these match the actual structure returned by your API
    private record LoginResponseDto(bool IsAuthenticated, bool IsVerified, int UserId, string Username, string Email, string Role);
    private record ErrorResponseDto(string Message);
    private record ForgotPasswordRequestDto(string EmailOrUsername);
    private record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);

} // End of AccountController class
