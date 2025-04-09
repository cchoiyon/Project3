using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Project3.Models.ViewModels;
using Project3.Models.InputModels;
using Project3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using Project3.Models.DTOs;
using System;
using System.Text.Json;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using Project3.Utilities;
using Dapper;
using System.Linq;
using System.Data;

namespace Project3.Controllers
{
    /// <summary>
    /// Controller responsible for handling user account actions like
    /// Login, Logout, Registration, Password Reset, etc.
    /// </summary>
    public class AccountController : Controller
    {
        // Dependency injected services
        private readonly ILogger<AccountController> _logger;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly DBConnect _dbConnect;
        private readonly Email _emailService;
        private readonly string _connectionString;

        /// <summary>
        /// Constructor to initialize the controller with required services.
        /// </summary>
        /// <param name="logger">Logger instance for logging information and errors.</param>
        /// <param name="userService">Service for user-related operations.</param>
        /// <param name="configuration">Configuration service for accessing app settings.</param>
        /// <param name="dbConnect">DBConnect for database operations.</param>
        /// <param name="emailService">Email service for sending emails.</param>
        public AccountController(
            ILogger<AccountController> logger,
            IUserService userService,
            IConfiguration configuration,
            DBConnect dbConnect,
            Email emailService)
        {
            _logger = logger;
            _userService = userService;
            _configuration = configuration;
            _dbConnect = dbConnect;
            _emailService = emailService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
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
                return RedirectToDashboard();
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
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await ValidateUserCredentials(model.Username, model.Password);
                    if (user != null)
                    {
                        _logger.LogInformation($"User {model.Username} logged in successfully");
                        
                        // Create claims with the mapped role name
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                            new Claim(ClaimTypes.Role, user.UserType) // UserType is already mapped to the correct role name
                        };
                        
                        _logger.LogInformation($"Setting claims for user {user.Username}: Name={user.Username}, ID={user.UserID}, Role={user.UserType}");
                        
                        // Create the claims identity with the correct authentication type
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        
                        // Create the claims principal
                        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                        
                        // Set authentication properties
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe
                        };
                        
                        // Sign in the user
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            claimsPrincipal,
                            authProperties);
                        
                        // Log the claims after signing in
                        _logger.LogInformation("Claims after signing in:");
                        foreach (var claim in claimsPrincipal.Claims)
                        {
                            _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
                        }
                        
                        return RedirectToDashboard();
                    }

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login attempt");
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }

            return View(model);
        }

        /// <summary>
        /// Validates user credentials by directly querying the database.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <param name="password">The password to validate.</param>
        /// <returns>The user object if credentials are valid, null otherwise.</returns>
        private async Task<dynamic> ValidateUserCredentials(string username, string password)
        {
            SqlCommand cmd = new SqlCommand("TP_spCheckUser");
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@UserPassword", password); // This is the plain password, the SP will handle hashing

            DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
            
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var row = ds.Tables[0].Rows[0];
                string storedHash = row["PasswordHash"]?.ToString();
                
                if (storedHash != null && BCrypt.Net.BCrypt.Verify(password, storedHash))
                {
                    // Get the user type and ensure it matches the expected role names
                    string userType = row["UserType"]?.ToString();
                    
                    // Map the user type to the correct role name using case-insensitive comparison
                    string role;
                    if (userType != null)
                    {
                        if (userType.ToLower() == "reviewer")
                        {
                            role = "Reviewer";
                        }
                        else if (userType.ToLower() == "restaurantrep")
                        {
                            role = "RestaurantRep";
                        }
                        else
                        {
                            role = userType; // Keep original if it doesn't match
                        }
                    }
                    else
                    {
                        role = "User"; // Default role if userType is null
                    }
                    
                    // Log the user type and role for debugging
                    _logger.LogInformation("User {Username} has UserType: {UserType}, Mapped to Role: {Role}", username, userType, role);
                    
                    return new
                    {
                        UserID = Convert.ToInt32(row["UserID"]),
                        Username = username,
                        UserType = role // Use the mapped role name
                    };
                }
            }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            // You might initialize a RegisterModel here if needed by the view
            // var model = new RegisterModel();
            return View(); // Renders Views/Account/Register.cshtml
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(); // Renders Views/Account/ForgotPassword.cshtml
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotUsername()
        {
            return View(); // Renders Views/Account/ForgotUsername.cshtml
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoginWith2fa()
        {
            // Implement two-factor authentication logic here
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // --- Helper Methods ---

        /// <summary>
        /// Determines the correct dashboard action based on the user's role.
        /// </summary>
        /// <returns>An IActionResult redirecting to the appropriate controller/action.</returns>
        private IActionResult RedirectToDashboard()
        {
            // Log all claims for debugging
            _logger.LogInformation("User claims:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            
            // Check roles using IsInRole
            bool isRestaurantRep = User.IsInRole("RestaurantRep");
            bool isReviewer = User.IsInRole("Reviewer");
            
            _logger.LogInformation($"User is in RestaurantRep role: {isRestaurantRep}");
            _logger.LogInformation($"User is in Reviewer role: {isReviewer}");
            
            // Redirect based on role
            if (isRestaurantRep)
            {
                _logger.LogInformation("Redirecting to RestaurantRepHome/Index");
                return RedirectToAction("Index", "RestaurantRepHome");
            }
            else if (isReviewer)
            {
                _logger.LogInformation("Redirecting to ReviewerHome/Index");
                return RedirectToAction("Index", "ReviewerHome");
            }
            else
            {
                _logger.LogWarning("User is not in any recognized role, redirecting to Home/Index");
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Gets the user's role from their claims.
        /// </summary>
        /// <returns>The user's role as a string, or null if not found.</returns>
        private string GetUserRole()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return null;
            }

            // Try to get the role claim
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            return roleClaim?.Value;
        }

        /// <summary>
        /// DTO for error responses from the API.
        /// </summary>
        private record ErrorResponseDto(string Message);

        // Add other DTO records used by this controller if needed, e.g.:
        // private record ForgotPasswordRequestDto(string EmailOrUsername);
        // private record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);

    } // End of AccountController class

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
    }
}
