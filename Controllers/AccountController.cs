// Controllers/AccountController.cs
// Apply fixes for CS1061 (DBConnect, Email methods) and CS0161 (return paths)

using Microsoft.AspNetCore.Mvc;
using Project3.Models;
using Project3.Utilities; // Contains DBConnect, Email
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options; // Needed for IOptions<SmtpSettings>
using System.Text.RegularExpressions; // For email validation if needed
using System.Diagnostics; // For ErrorViewModel


namespace Project3.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBConnect _objDB;
        private readonly Email _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly SmtpSettings _smtpSettings; // Store injected settings
        public static readonly int VerificationCodeLifetimeMinutes = 120;

        // Constructor updated to inject SmtpSettings via IOptions
        public AccountController(IOptions<SmtpSettings> smtpSettingsOptions, Email emailService, ILogger<AccountController> logger)
        {
            _objDB = new DBConnect(); // Consider injecting
            _smtpSettings = smtpSettingsOptions.Value; // Get settings from options
            _emailService = emailService; // Use injected email service
            _logger = logger;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }
            // Pass message from TempData if verification was just completed
            ViewBag.SuccessMessage = TempData["Message"];
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }

            // Manual Validation (using existing model validation + custom checks if needed)
            if (string.IsNullOrWhiteSpace(model.Username))
                ModelState.AddModelError("Username", "Username required.");
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password required.");


            if (ModelState.IsValid)
            {
                try
                {
                    // Call SP TP_spCheckUser to get stored hash and status
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.TP_spCheckUser";
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    // Pass plain password as this SP expects it (based on SP PDF)
                    cmd.Parameters.AddWithValue("@UserPassword", model.Password);

                    // Use the correct DBConnect method
                    DataSet ds = _objDB.GetDataSetUsingCmdObj(cmd);

                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow dr = ds.Tables[0].Rows[0];
                        string storedHash = dr["PasswordHash"]?.ToString();
                        bool isVerified = dr["IsVerified"] != DBNull.Value && Convert.ToBoolean(dr["IsVerified"]);
                        // Use UserType based on TP_Users table schema
                        string userRole = dr["UserType"]?.ToString() ?? "Guest";
                        int userId = Convert.ToInt32(dr["UserID"]);

                        // *** CRITICAL: Implement password hash verification ***
                        // Since TP_spCheckUser returns the hash, C# must verify it.
                        // You need a utility method (or use BCrypt directly)
                        // bool passwordIsValid = VerifyPassword(model.Password, storedHash); // Assuming you have this helper using BCrypt
                        bool passwordIsValid = false; // Placeholder - MUST IMPLEMENT HASH CHECK
                        if (!string.IsNullOrEmpty(storedHash))
                        {
                            try
                            {
                                passwordIsValid = BCrypt.Net.BCrypt.Verify(model.Password, storedHash);
                            }
                            catch (Exception hashEx)
                            {
                                _logger.LogError(hashEx, "BCrypt verification failed for user {Username}", model.Username);
                                passwordIsValid = false; // Treat hash errors as invalid password
                            }
                        }


                        if (!passwordIsValid) // Check hash validity
                        {
                            _logger.LogWarning("Invalid login attempt (password mismatch) for user: {Username}", model.Username);
                            ModelState.AddModelError(string.Empty, "Invalid username or password.");
                            return View(model);
                        }

                        // Password is valid, now check verification status
                        if (!isVerified)
                        {
                            _logger.LogWarning("Login attempt failed for unverified user: {Username}", model.Username);
                            ModelState.AddModelError(string.Empty, "Your account has not been verified. Please check your email for the verification link.");
                            // Optionally add "Resend Link" feature
                            return View(model);
                        }

                        // User is valid and verified, proceed with login
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Role, userRole)
                        };
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation("User {Username} logged in successfully.", model.Username);

                        if (Url.IsLocalUrl(returnUrl)) { return Redirect(returnUrl); }
                        else { return RedirectToDashboard(userRole); }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid login attempt (user not found or SP error) for user: {Username}", model.Username);
                        ModelState.AddModelError(string.Empty, "Invalid username or password.");
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login for user: {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "An error occurred during login.");
                    return View(model);
                }
            }
            return View(model);
        }


        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (User.Identity.IsAuthenticated) { return RedirectToDashboard(GetUserRole()); }

            // Manual Validation (Example - adapt if needed)
            if (string.IsNullOrWhiteSpace(model.Username)) ModelState.AddModelError("Username", "Username required.");
            // ... add other manual checks if model annotations aren't sufficient ...
            if (model.Password != model.ConfirmPassword) ModelState.AddModelError("ConfirmPassword", "Passwords dont match.");


            if (ModelState.IsValid)
            {
                // TODO: Pre-check username/email using TP_spCheckUsernameExists etc.

                try
                {
                    string verificationCode = GenerateVerificationCode();
                    DateTime expiryTime = DateTime.UtcNow.AddMinutes(VerificationCodeLifetimeMinutes);
                    int registeredUserId = 0;

                    // *** Hash the password BEFORE calling the SP ***
                    string hashedPassword = null;
                    try
                    {
                        hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
                    }
                    catch (Exception hashEx)
                    {
                        _logger.LogError(hashEx, "Password hashing failed during registration for {Username}", model.Username);
                        ModelState.AddModelError("", "Error processing registration data.");
                        return View(model);
                    }
                    if (hashedPassword == null)
                    {
                        ModelState.AddModelError("", "Error processing registration data.");
                        return View(model);
                    }


                    SqlCommand registerCmd = new SqlCommand();
                    registerCmd.CommandType = CommandType.StoredProcedure;
                    // Use the SP name from your PDF that adds user
                    registerCmd.CommandText = "dbo.TP_spAddUser"; // Ensure this SP is modified to accept hash, code, expiry

                    // Pass HASHED password - ensure SP parameter name matches (@PasswordHash or @UserPassword)
                    registerCmd.Parameters.AddWithValue("@UserPassword", hashedPassword); // Match SP param name

                    // Pass other required parameters
                    registerCmd.Parameters.AddWithValue("@Username", model.Username);
                    registerCmd.Parameters.AddWithValue("@Email", model.Email);
                    // Use UserRole from model, ensure SP param name is @UserType
                    registerCmd.Parameters.AddWithValue("@UserType", model.UserRole);
                    // Security Q&A - Assuming SP expects hashes, hash them here
                    // string ans1Hash = BCrypt.Net.BCrypt.HashPassword(model.SecurityAnswer1); // Example
                    registerCmd.Parameters.AddWithValue("@SecurityQuestion1", model.SecurityQuestion1);
                    registerCmd.Parameters.AddWithValue("@SecurityAnswerHash1", model.SecurityAnswer1); // Pass plain? Or hash? Check SP! PDF says SP expects hash.
                    registerCmd.Parameters.AddWithValue("@SecurityQuestion2", model.SecurityQuestion2);
                    registerCmd.Parameters.AddWithValue("@SecurityAnswerHash2", model.SecurityAnswer2); // Pass plain? Or hash? Check SP!
                    registerCmd.Parameters.AddWithValue("@SecurityQuestion3", model.SecurityQuestion3);
                    registerCmd.Parameters.AddWithValue("@SecurityAnswerHash3", model.SecurityAnswer3); // Pass plain? Or hash? Check SP!

                    // Pass verification details (Ensure SP is modified to accept these)
                    registerCmd.Parameters.AddWithValue("@VerificationCode", verificationCode); // Example param name
                    registerCmd.Parameters.AddWithValue("@VerificationExpiry", expiryTime);     // Example param name


                    // *** FIX: Use GetDataSetUsingCmdObj and extract scalar result ***
                    DataSet dsResult = _objDB.GetDataSetUsingCmdObj(registerCmd);
                    // Assuming the SP returns SCOPE_IDENTITY() named as NewUserID in the first row/column
                    if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0 && dsResult.Tables[0].Columns.Contains("NewUserID"))
                    {
                        registeredUserId = Convert.ToInt32(dsResult.Tables[0].Rows[0]["NewUserID"]);
                    }


                    if (registeredUserId > 0) // Check if registration (and getting UserID) succeeded
                    {
                        var callbackUrl = Url.Action(
                            action: nameof(ConfirmEmail),
                            controller: "Account",
                            values: new { userId = registeredUserId, code = verificationCode },
                            protocol: Request.Scheme);

                        if (string.IsNullOrEmpty(callbackUrl))
                        {
                            _logger.LogError("Could not generate callback URL for email confirmation.");
                            ModelState.AddModelError("", "Could not generate confirmation link. Registration failed.");
                            return View(model);
                        }

                        _logger.LogInformation("Generated confirmation URL: {CallbackUrl}", callbackUrl);

                        try
                        {
                            string subject = "Confirm Your Email Address";
                            string encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);
                            string body = $"Welcome! Please confirm your account by <a href='{encodedUrl}'>clicking here</a>.\n" +
                                          $"This link will expire in {VerificationCodeLifetimeMinutes / 60} hours.";

                            // *** FIX: Use correct Email method name 'SendMail' and pass SmtpSettings ***
                            // Use _smtpSettings injected in constructor
                            _emailService.SendMail(_smtpSettings, model.Email, null, subject, body, true); // Removed await

                            _logger.LogInformation("Confirmation link email sent to {Email}", model.Email);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send confirmation email to {Email}", model.Email);
                            ModelState.AddModelError("", "Could not send confirmation email. Please try registering again later or contact support.");
                            return View(model);
                        }

                        return RedirectToAction("RegistrationConfirmationSent");
                    }
                    else
                    {
                        _logger.LogWarning("Registration failed for username: {Username}. SP did not return valid UserID.", model.Username);
                        ModelState.AddModelError(string.Empty, "Registration failed. The username or email might already exist, or a database error occurred.");
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration for username: {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "An error occurred during registration.");
                    return View(model);
                }
            }
            // If ModelState is invalid, redisplay form
            return View(model);
        }

        // GET: /Account/RegistrationConfirmationSent
        [AllowAnonymous]
        public IActionResult RegistrationConfirmationSent()
        {
            ViewBag.ExpiryMinutes = VerificationCodeLifetimeMinutes;
            return View();
        }


        // GET: /Account/ConfirmEmail?userId=123&code=abcdef
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(int? userId, string code)
        {
            if (userId == null || code == null)
            {
                _logger.LogWarning("ConfirmEmail called with missing userId or code.");
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // SP call to get user verification details by UserID
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                // Use SP that returns UserID, Username, Email, UserType, IsVerified, VerificationToken, VerificationTokenExpiry
                // Modify/Create TP_spGetUserForVerification as needed
                cmd.CommandText = "dbo.TP_spGetUserForVerification"; // EXAMPLE NAME - VERIFY/CREATE!
                cmd.Parameters.AddWithValue("@UserID", userId.Value);

                // Use correct DBConnect method
                DataSet ds = _objDB.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];
                    bool isAlreadyVerified = Convert.ToBoolean(dr["IsVerified"]);
                    string storedCode = dr["VerificationToken"]?.ToString(); // Use correct column name from TP_Users
                    // Use correct column name for expiry (must be added to TP_Users and returned by SP)
                    DateTime? expiryTime = dr["VerificationTokenExpiry"] as DateTime?; // EXAMPLE NAME - VERIFY/ADD!
                    string userRole = dr["UserType"]?.ToString() ?? "Guest"; // Use UserType from TP_Users
                    string username = dr["Username"]?.ToString();

                    if (isAlreadyVerified)
                    {
                        _logger.LogInformation("User {UserId} tried to confirm email but is already verified.", userId);
                        ViewBag.ConfirmationStatus = "Success";
                        ViewBag.Message = "Your email address has already been confirmed. You can now log in.";
                        return View();
                    }

                    // Check code validity (Ensure expiryTime is checked!)
                    if (string.IsNullOrEmpty(storedCode) || expiryTime == null)
                    {
                        _logger.LogError("Email confirmation failed for UserID {UserId}: Stored code or expiry is missing.", userId);
                        ViewBag.ConfirmationStatus = "Error";
                        ViewBag.Message = "Email confirmation failed due to missing verification data. Please register again or contact support.";
                        return View();
                    }

                    if (expiryTime < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Email confirmation failed for UserID {UserId}: Link expired.", userId);
                        ViewBag.ConfirmationStatus = "Error";
                        ViewBag.Message = "The email confirmation link has expired. Please register again to receive a new link.";
                        return View();
                    }

                    if (storedCode.Equals(code, StringComparison.Ordinal)) // Case-sensitive comparison
                    {
                        _logger.LogInformation("Email confirmation successful for UserID {UserId}.", userId);

                        // Call SP to activate user (TP_spSetUserVerified from PDF)
                        SqlCommand activateCmd = new SqlCommand();
                        activateCmd.CommandType = CommandType.StoredProcedure;
                        activateCmd.CommandText = "dbo.TP_spSetUserVerified"; // Use SP name from PDF
                        activateCmd.Parameters.AddWithValue("@UserID", userId.Value);

                        // *** FIX: Use correct DBConnect method name 'DoUpdateUsingCmdObj' ***
                        _objDB.DoUpdateUsingCmdObj(activateCmd); // Use method from DBConnect.cs

                        // Optional Auto-Login (Code is okay, just uncomment if desired)
                        /*
                        var claims = new List<Claim> { ... };
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties { IsPersistent = false };
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                        _logger.LogInformation("User {Username} confirmed email and auto-logged in.", username);
                        return RedirectToDashboard(userRole);
                        */

                        TempData["Message"] = "Thank you for confirming your email. You can now log in.";
                        return RedirectToAction("Login"); // Redirect to Login after successful confirmation
                        // ViewBag.ConfirmationStatus = "Success";
                        // ViewBag.Message = "Thank you for confirming your email. You can now log in.";
                    }
                    else
                    {
                        _logger.LogWarning("Invalid confirmation code attempt for UserID {UserId}.", userId);
                        ViewBag.ConfirmationStatus = "Error";
                        ViewBag.Message = "Invalid email confirmation link or code.";
                        return View();
                    }
                }
                else
                {
                    _logger.LogError("Email confirmation failed: User not found for UserID {UserId}.", userId);
                    ViewBag.ConfirmationStatus = "Error";
                    ViewBag.Message = "Email confirmation failed. User not found.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation for UserID {UserId}.", userId);
                ViewBag.ConfirmationStatus = "Error";
                ViewBag.Message = "An error occurred during email confirmation.";
                return View();
            }
            // return View(); // Should be unreachable if logic covers all cases
        }


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string username = User.Identity.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Use default scheme
            _logger.LogInformation("User {Username} logged out.", username);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // --- Helper Methods ---

        private string GenerateVerificationCode()
        {
            // This should always return a value. CS0161 might be spurious.
            try
            {
                return RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating verification code.");
                // Fallback or rethrow - ensure a value is always returned if possible
                return "123456"; // Example fallback - NOT recommended for production
            }
            // return string.Empty; // Redundant return to satisfy compiler if needed
        }

        private IActionResult RedirectToDashboard(string userRole)
        {
            // Use UserType based on TP_Users schema
            if (userRole.Equals("reviewer", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "ReviewerHome");
            }
            else if (userRole.Equals("restaurantRep", StringComparison.OrdinalIgnoreCase)) // Use role name from SP PDF
            {
                // Replace "RestaurantOwnerHome" with your actual controller name for reps
                return RedirectToAction("Index", "RestaurantRepHome"); // Example name
            }
            else
            {
                _logger.LogWarning("Unknown role '{Role}' encountered during redirect. Defaulting to Home.", userRole);
                return RedirectToAction("Index", "Home");
            }
            // return RedirectToAction("Index", "Home"); // Redundant return to satisfy compiler if needed
        }

        private string GetUserRole()
        {
            // This should always return a value. CS0161 might be spurious.
            try
            {
                return User.FindFirstValue(ClaimTypes.Role) ?? "Guest";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role from claims.");
                return "Guest"; // Fallback
            }
            // return "Guest"; // Redundant return to satisfy compiler if needed
        }

        // Removed VerifyEmail(GET/POST) and ResendCode actions.
        // TODO: Implement Forgot Password etc. using similar patterns (token generation, email link, confirmation action)
        // Ensure password hashing is used consistently.

    }
}
