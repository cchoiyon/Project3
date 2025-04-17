using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Project3.Shared.Models.ViewModels;
using Project3.Shared.Models.InputModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using Project3.Shared.Models.DTOs;
using System;
using System.Text.Json;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using Project3.Shared.Utilities;
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
        private readonly IConfiguration _configuration;
        private readonly Connection _dbConnect;
        private readonly Email _emailService;
        private readonly string _connectionString;

        /// <summary>
        /// Constructor to initialize the controller with required services.
        /// </summary>
        /// <param name="logger">Logger instance for logging information and errors.</param>
        /// <param name="configuration">Configuration service for accessing app settings.</param>
        /// <param name="dbConnect">Connection for database operations.</param>
        /// <param name="emailService">Email service for sending emails.</param>
        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration configuration,
            Connection dbConnect,
            Email emailService)
        {
            _logger = logger;
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
                    // Check if user exists and is verified
                    SqlCommand cmd = new SqlCommand("SELECT UserID, Username, PasswordHash, UserType, Email, ISNULL(IsVerified, 0) AS IsVerified FROM TP_Users WHERE Username = @Username");
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    
                    var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                    
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        var row = ds.Tables[0].Rows[0];
                        string storedHash = row["PasswordHash"]?.ToString();
                        bool isVerified = Convert.ToBoolean(row["IsVerified"]);
                        string email = row["Email"]?.ToString();
                        
                        if (storedHash != null && BCrypt.Net.BCrypt.Verify(model.Password, storedHash))
                        {
                            // TEMPORARILY BYPASSING EMAIL VERIFICATION CHECK FOR TESTING
                            // Comment out the verification check block
                            /*
                            // Check if account is verified
                            if (!isVerified)
                            {
                                // Generate a new verification code
                                string verificationCode = new Random().Next(100000, 999999).ToString();
                                
                                // Update the verification code
                                SqlCommand updateCmd = new SqlCommand("UPDATE TP_Users SET VerificationCode = @VerificationCode WHERE Username = @Username");
                                updateCmd.Parameters.AddWithValue("@VerificationCode", verificationCode);
                                updateCmd.Parameters.AddWithValue("@Username", model.Username);
                                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                                
                                // Send verification email
                                await SendVerificationEmail(email, model.Username, verificationCode);
                                
                                TempData["Email"] = email;
                                TempData["Message"] = "Your account is not verified. A new verification code has been sent to your email.";
                                return RedirectToAction("VerifyEmail");
                            }
                            */
                            
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
                            
                            // Create claims with the mapped role name
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, model.Username),
                                new Claim(ClaimTypes.NameIdentifier, row["UserID"].ToString()),
                                new Claim(ClaimTypes.Role, role) // UserType is already mapped to the correct role name
                            };
                            
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
                            
                            _logger.LogInformation("User {Username} logged in successfully", model.Username);
                            
                            return RedirectToDashboard();
                        }
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

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username already exists
                    SqlCommand checkUsernameCmd = new SqlCommand("SELECT COUNT(*) FROM TP_Users WHERE Username = @Username");
                    checkUsernameCmd.Parameters.AddWithValue("@Username", model.Username);
                    int usernameCount = Convert.ToInt32(_dbConnect.ExecuteScalarUsingCmdObj(checkUsernameCmd));
                    
                    if (usernameCount > 0)
                    {
                        ModelState.AddModelError("Username", "This username is already taken. Please choose another.");
                        return View(model);
                    }
                    
                    // Check if email already exists
                    SqlCommand checkEmailCmd = new SqlCommand("SELECT COUNT(*) FROM TP_Users WHERE Email = @Email");
                    checkEmailCmd.Parameters.AddWithValue("@Email", model.Email);
                    int emailCount = Convert.ToInt32(_dbConnect.ExecuteScalarUsingCmdObj(checkEmailCmd));
                    
                    if (emailCount > 0)
                    {
                        ModelState.AddModelError("Email", "This email is already registered.");
                        return View(model);
                    }
                    
                    // Hash the password
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    
                    // Generate a random 6-digit verification code
                    string verificationCode = new Random().Next(100000, 999999).ToString();
                    
                    // Update the stored procedure call to include verification code
                    SqlCommand cmd = new SqlCommand("TP_spCreateUser");
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", model.LastName);
                    cmd.Parameters.AddWithValue("@UserType", model.UserRole); // Use the selected role
                    cmd.Parameters.AddWithValue("@SecurityQuestion1", model.SecurityQuestion1);
                    cmd.Parameters.AddWithValue("@SecurityAnswer1", model.SecurityAnswer1);
                    cmd.Parameters.AddWithValue("@SecurityQuestion2", model.SecurityQuestion2);
                    cmd.Parameters.AddWithValue("@SecurityAnswer2", model.SecurityAnswer2);
                    cmd.Parameters.AddWithValue("@SecurityQuestion3", model.SecurityQuestion3);
                    cmd.Parameters.AddWithValue("@SecurityAnswer3", model.SecurityAnswer3);
                    
                    int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                    
                    if (result > 0)
                    {
                        _logger.LogInformation("User {Username} registered successfully", model.Username);
                        
                        // TEMPORARILY BYPASS EMAIL VERIFICATION FOR TESTING
                        // After creating the user, set the user as verified immediately
                        SqlCommand updateCmd = new SqlCommand("UPDATE TP_Users SET IsVerified = 1 WHERE Username = @Username");
                        updateCmd.Parameters.AddWithValue("@Username", model.Username);
                        _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                        
                        // Automatically sign in the user
                        var userType = model.UserRole;
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
                                role = userType;
                            }
                        }
                        else
                        {
                            role = "User";
                        }
                        
                        // Get the user ID
                        SqlCommand getUserIdCmd = new SqlCommand("SELECT UserID FROM TP_Users WHERE Username = @Username");
                        getUserIdCmd.Parameters.AddWithValue("@Username", model.Username);
                        var userId = _dbConnect.ExecuteScalarUsingCmdObj(getUserIdCmd);
                        
                        // Create claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Role, role)
                        };
                        
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true
                        };
                        
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                        
                        return RedirectToDashboard();
                        
                        /* COMMENTING OUT EMAIL VERIFICATION FLOW FOR TESTING
                        // After creating the user, update the verification code in the database
                        SqlCommand updateCmd = new SqlCommand("UPDATE TP_Users SET VerificationCode = @VerificationCode, IsVerified = 0 WHERE Username = @Username");
                        updateCmd.Parameters.AddWithValue("@VerificationCode", verificationCode);
                        updateCmd.Parameters.AddWithValue("@Username", model.Username);
                        _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                        
                        // Try to send verification email
                        bool emailSent = true;
                        try 
                        {
                            await SendVerificationEmail(model.Email, model.Username, verificationCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send verification email to {Email}", model.Email);
                            emailSent = false;
                            
                            // For debugging in development, let's show the verification code in TempData
                            if (_configuration["Environment"]?.ToLower() == "development" || 
                                _configuration.GetValue<bool>("IsDevelopment"))
                            {
                                TempData["DevelopmentVerificationCode"] = verificationCode;
                            }
                        }
                        
                        // Store email in TempData for the verification page
                        TempData["Email"] = model.Email;
                        
                        if (!emailSent)
                        {
                            TempData["EmailSendingError"] = "We couldn't send the verification email. Please check your network connection and try again later.";
                        }
                        
                        // Redirect to verification page
                        return RedirectToAction("VerifyEmail");
                        */
                    }
                    else
                    {
                        _logger.LogWarning("Failed to register user {Username}", model.Username);
                        ModelState.AddModelError("", "Registration failed. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during user registration");
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail()
        {
            // If no email in TempData, redirect to login
            if (TempData["Email"] == null)
            {
                return RedirectToAction("Login");
            }
            
            // Keep the email in TempData for the post action
            TempData.Keep("Email");
            
            var model = new VerifyEmailModel
            {
                Email = TempData["Email"].ToString()
            };
            
            // For development purposes, show the verification code if available
            if (TempData["DevelopmentVerificationCode"] != null)
            {
                TempData["Message"] = $"DEVELOPMENT MODE: Verification code is {TempData["DevelopmentVerificationCode"]}";
                TempData.Keep("DevelopmentVerificationCode");
            }
            
            // Display any email sending errors
            if (TempData["EmailSendingError"] != null)
            {
                TempData["ErrorMessage"] = TempData["EmailSendingError"];
                TempData.Remove("EmailSendingError");
            }
            
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if verification code matches
                    SqlCommand cmd = new SqlCommand("SELECT UserID, UserType, Username FROM TP_Users WHERE Email = @Email AND VerificationCode = @VerificationCode");
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@VerificationCode", model.VerificationCode);
                    
                    var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                    
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        // Get user info
                        var row = ds.Tables[0].Rows[0];
                        int userId = Convert.ToInt32(row["UserID"]);
                        string userType = row["UserType"].ToString();
                        string username = row["Username"].ToString();
                        
                        // Update user as verified
                        SqlCommand updateCmd = new SqlCommand("UPDATE TP_Users SET IsVerified = 1, VerificationCode = NULL WHERE UserID = @UserID");
                        updateCmd.Parameters.AddWithValue("@UserID", userId);
                        _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                        
                        // Auto login the user
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, username),
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Role, userType)
                        };
                        
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true
                        };
                        
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);
                        
                        _logger.LogInformation("User {UserId} verified email successfully", userId);
                        
                        // Redirect to appropriate homepage based on role
                        return RedirectToDashboard();
                    }
                    
                    ModelState.AddModelError("VerificationCode", "Invalid verification code. Please try again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying email for user {Email}", model.Email);
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                }
            }
            
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationCode(string email)
        {
            try
            {
                // Get the username for this email
                SqlCommand getUserCmd = new SqlCommand("SELECT Username FROM TP_Users WHERE Email = @Email");
                getUserCmd.Parameters.AddWithValue("@Email", email);
                var ds = _dbConnect.GetDataSetUsingCmdObj(getUserCmd);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    string username = ds.Tables[0].Rows[0]["Username"].ToString();
                    
                    // Generate a new verification code
                    string verificationCode = new Random().Next(100000, 999999).ToString();
                    
                    // Update the verification code in the database
                    SqlCommand cmd = new SqlCommand("UPDATE TP_Users SET VerificationCode = @VerificationCode WHERE Email = @Email");
                    cmd.Parameters.AddWithValue("@VerificationCode", verificationCode);
                    cmd.Parameters.AddWithValue("@Email", email);
                    int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                    
                    if (result > 0)
                    {
                        // Send a new verification email
                        await SendVerificationEmail(email, username, verificationCode);
                        
                        TempData["Message"] = "A new verification code has been sent to your email.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Could not resend verification code. Email address not found.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Email address not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification code to {Email}", email);
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
            }
            
            TempData["Email"] = email;
            return RedirectToAction("VerifyEmail");
        }
        
        // Helper method to send verification email
        private async Task SendVerificationEmail(string email, string username, string code)
        {
            string subject = "Verify Your Account - Restaurant Reviews";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Welcome to Restaurant Reviews!</h2>
                    <p>Hello {username},</p>
                    <p>Thank you for registering with us. Please use the verification code below to verify your account:</p>
                    <h3 style='background-color: #f5f5f5; padding: 10px; text-align: center;'>{code}</h3>
                    <p>This code will expire in 24 hours.</p>
                    <p>If you did not create this account, please ignore this email.</p>
                    <p>Regards,<br/>Restaurant Reviews Team</p>
                </body>
                </html>";
            
            await _emailService.SendEmailAsync(email, subject, body);
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
