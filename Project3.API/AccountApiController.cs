using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Project3.Shared.Models.Configuration;
// using Project3.Shared.Models.DTOs; // Create and use DTOs for API contracts
using Project3.Shared.Models.InputModels; // Can use InputModels if they match API needs
using Project3.Shared.Utilities; // For Connection and Email service
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Security.Cryptography; // For token generation if done here
using System.Net; // *** ADDED/VERIFIED: For WebUtility ***

// Ensure this namespace matches your project structure
namespace Project3.API.Controllers
{
    [Route("api/[controller]")] // Route will be /api/AccountApi
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly ILogger<AccountApiController> _logger;
        private readonly Connection _dbConnect; // Injected via DI
        private readonly Email _emailService; // Injected via DI
        private readonly SmtpSettings _smtpSettings; // Injected via DI (IOptions<SmtpSettings>)
        private readonly IConfiguration _configuration; // Injected via DI

        // DTO Definitions (Consider moving to a shared Models/DTOs folder/project)
        public record LoginResponseDto(bool IsAuthenticated, bool IsVerified, int UserId, string Username, string Email, string Role);
        public record ErrorResponseDto(string Message);
        public record VerificationRequestDto(string VerificationToken);
        public record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);
        public record ForgotPasswordRequestDto(string EmailOrUsername);
        public record RegisterRequestDto(
                 string Username, string Email, string Password, string UserRole, string FirstName, string LastName,
                 string SecurityQuestion1, string SecurityAnswerHash1,
                 string SecurityQuestion2, string SecurityAnswerHash2,
                 string SecurityQuestion3, string SecurityAnswerHash3
        );


        // Updated Constructor to inject IConfiguration
        public AccountApiController(
            ILogger<AccountApiController> logger,
            Connection dbConnect, // Receives instance from DI
            Email emailService, // Receives instance from DI
            IOptions<SmtpSettings> smtpSettingsOptions, // Receives configuration from DI
            IConfiguration configuration) // Added IConfiguration parameter
        {
            _logger = logger;
            _dbConnect = dbConnect;
            _emailService = emailService;
            _smtpSettings = smtpSettingsOptions.Value; // Get the actual settings object
            _configuration = configuration; // Assign injected configuration to field
        }

        // POST: api/AccountApi/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginModel loginModel)
        {
            _logger.LogInformation("API: Login attempt for username {Username}", loginModel.Username);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            DataSet ds = null;

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spCheckUser");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", loginModel.Username);
                // *** ADDED Missing Parameter ***
                cmd.Parameters.AddWithValue("@UserPassword", loginModel.Password);
                // *** END FIX ***

                // Attempt the database call using the corrected Connection method
                ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                // Check if user was found
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];
                    string storedHash = dr["PasswordHash"]?.ToString();
                    bool isVerified = Convert.ToBoolean(dr["IsVerified"]);
                    string userRole = dr["UserType"]?.ToString() ?? "Guest";
                    int userId = Convert.ToInt32(dr["UserID"]);
                    string email = dr.Table.Columns.Contains("Email") ? dr["Email"]?.ToString() : string.Empty;

                    // Verify password
                    bool passwordIsValid = false;
                    if (!string.IsNullOrEmpty(storedHash))
                    {
                        try { passwordIsValid = BCrypt.Net.BCrypt.Verify(loginModel.Password, storedHash); }
                        catch (Exception hashEx) { _logger.LogError(hashEx, "BCrypt verification failed during login for {Username} with exception.", loginModel.Username); }
                    }
                    else { _logger.LogWarning("API: Password hash not found in DB for user {Username}", loginModel.Username); }

                    if (!passwordIsValid)
                    {
                        _logger.LogWarning("API: BCrypt.Verify returned false for user {Username}. Password check failed.", loginModel.Username);
                        _logger.LogWarning("API: Invalid password provided for user {Username}", loginModel.Username);
                        return Unauthorized(new ErrorResponseDto("Invalid username or password.")); // 401
                    }

                    // Success
                    var responseDto = new LoginResponseDto(true, isVerified, userId, loginModel.Username, email, userRole);
                    _logger.LogInformation("API: Login successful for user {Username}", loginModel.Username);
                    return Ok(responseDto);
                }
                else
                {
                    // User not found (query succeeded but returned no rows)
                    _logger.LogWarning("API: User not found for username {Username} (DB query returned no rows).", loginModel.Username);
                    return Unauthorized(new ErrorResponseDto("Invalid username or password.")); // 401 User not found
                }
            }
            catch (SqlException sqlEx) // Catch specific SQL errors from Connection methods
            {
                _logger.LogError(sqlEx, "API: SQL Error during login for {Username}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    loginModel.Username, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal database error occurred during login. Please check logs.")); // Return 500
            }
            catch (Exception ex) // Catch other unexpected errors
            {
                _logger.LogError(ex, "API: General Error during login for {Username}", loginModel.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal error occurred during login.")); // Return 500
            }
        }

        // POST: api/AccountApi/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registrationData)
        {
            _logger.LogInformation("API: Registration attempt for username {Username}", registrationData.Username);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                string verificationCode = GenerateSecureToken();
                DateTime expiryTime = DateTime.UtcNow.AddHours(24);
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registrationData.Password);

                SqlCommand cmd = new SqlCommand("dbo.TP_spAddUser");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Username", registrationData.Username);
                cmd.Parameters.AddWithValue("@Email", registrationData.Email);
                cmd.Parameters.AddWithValue("@UserPassword", hashedPassword);
                cmd.Parameters.AddWithValue("@UserType", registrationData.UserRole);
                cmd.Parameters.AddWithValue("@SecurityQuestion1", registrationData.SecurityQuestion1);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash1", registrationData.SecurityAnswerHash1);
                cmd.Parameters.AddWithValue("@SecurityQuestion2", registrationData.SecurityQuestion2);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash2", registrationData.SecurityAnswerHash2);
                cmd.Parameters.AddWithValue("@SecurityQuestion3", registrationData.SecurityQuestion3);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash3", registrationData.SecurityAnswerHash3);
                cmd.Parameters.AddWithValue("@VerificationToken", verificationCode);
                cmd.Parameters.AddWithValue("@VerificationTokenExpiry", expiryTime);

                object result = _dbConnect.ExecuteScalarFunction(cmd);

                int registeredUserId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                if (registeredUserId > 0)
                {
                    _logger.LogInformation("API: User {Username} registered with ID {UserId}. Sending verification email.", registrationData.Username, registeredUserId);
                    await SendConfirmationEmail(registeredUserId, registrationData.Email, verificationCode);
                    return Ok(new { Message = "Registration successful. Please check your email to verify your account." });
                }
                else
                {
                    _logger.LogError("API: User registration failed for {Username} (ExecuteScalar returned null/0 or DB error).", registrationData.Username);
                    return BadRequest(new ErrorResponseDto("Registration failed. Could not create user record."));
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error during registration for {Username}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    registrationData.Username, sqlEx.Number, sqlEx.Message);
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    return BadRequest(new ErrorResponseDto("Registration failed. Username or email may already exist."));
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal database error occurred during registration. Please check logs."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error during registration for {Username}", registrationData.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal error occurred during registration."));
            }
        }

        // POST: api/AccountApi/confirm-email
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] VerificationRequestDto verificationDto)
        {
            _logger.LogInformation("API: Email confirmation attempt for token starting {TokenStart}", verificationDto.VerificationToken?.Substring(0, Math.Min(verificationDto.VerificationToken?.Length ?? 0, 6)));
            if (string.IsNullOrEmpty(verificationDto.VerificationToken)) { return BadRequest(new ErrorResponseDto("Verification token is required.")); }

            DataSet dsUser = null;
            int userId = 0;

            try
            {
                SqlCommand checkCmd = new SqlCommand("dbo.TP_spGetUserForVerification");
                checkCmd.CommandType = CommandType.StoredProcedure;
                checkCmd.Parameters.AddWithValue("@VerificationToken", verificationDto.VerificationToken);
                dsUser = _dbConnect.GetDataSetUsingCmdObj(checkCmd);

                if (dsUser == null || dsUser.Tables.Count == 0 || dsUser.Tables[0].Rows.Count == 0)
                {
                    _logger.LogWarning("API: Invalid verification token provided (not found). Token starts: {TokenStart}", verificationDto.VerificationToken.Substring(0, Math.Min(verificationDto.VerificationToken.Length, 6)));
                    return BadRequest(new ErrorResponseDto("Invalid or expired confirmation link."));
                }

                DataRow dr = dsUser.Tables[0].Rows[0];
                bool isAlreadyVerified = Convert.ToBoolean(dr["IsVerified"]);
                DateTime? expiryTime = dr["VerificationTokenExpiry"] as DateTime?;
                userId = Convert.ToInt32(dr["UserID"]);

                if (isAlreadyVerified)
                {
                    _logger.LogInformation("API: Email already verified for User {UserId}.", userId);
                    return Ok(new { Message = "Email address already confirmed." });
                }

                if (expiryTime == null || expiryTime < DateTime.UtcNow)
                {
                    _logger.LogWarning("API: Expired verification token provided for User {UserId}. Token starts: {TokenStart}", userId, verificationDto.VerificationToken.Substring(0, Math.Min(verificationDto.VerificationToken.Length, 6)));
                    return BadRequest(new ErrorResponseDto("Confirmation link has expired."));
                }

                SqlCommand activateCmd = new SqlCommand("dbo.TP_spSetUserVerified");
                activateCmd.CommandType = CommandType.StoredProcedure;
                activateCmd.Parameters.AddWithValue("@UserID", userId);
                int result = _dbConnect.DoUpdateUsingCmdObj(activateCmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Email successfully verified for User {UserId}.", userId);
                    return Ok(new { Message = "Email successfully confirmed." });
                }
                else
                {
                    _logger.LogError("API: Failed to update user verification status for User {UserId} (DoUpdate returned 0).", userId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Failed to update verification status."));
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error during email confirmation process for User {UserId} (if known) / Token starting {TokenStart}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    userId > 0 ? userId.ToString() : "Unknown",
                    verificationDto.VerificationToken.Substring(0, Math.Min(verificationDto.VerificationToken.Length, 6)),
                    sqlEx.Number,
                    sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal database error occurred during email confirmation. Please check logs."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error during email confirmation for token starting {TokenStart}", verificationDto.VerificationToken.Substring(0, Math.Min(verificationDto.VerificationToken.Length, 6)));
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal error occurred during email confirmation."));
            }
        }


        // --- TODO: Implement Forgot Password / Reset Password / Forgot Username Endpoints ---


        // --- Helper Methods ---
        private async Task SendConfirmationEmail(int userId, string userEmail, string verificationCode)
        {
            string mvcAppBaseUrl = Request?.Scheme + "://" + Request?.Host.Value;
            if (string.IsNullOrEmpty(mvcAppBaseUrl))
            {
                mvcAppBaseUrl = _configuration["ApplicationBaseUrl"] ?? "https://localhost:7123";
                _logger.LogWarning("Could not determine request base URL dynamically, using fallback: {FallbackUrl}", mvcAppBaseUrl);
            }

            var callbackUrl = $"{mvcAppBaseUrl}/Account/ConfirmEmail?code={WebUtility.UrlEncode(verificationCode)}";
            _logger.LogInformation("Generated confirmation URL for API: {CallbackUrl}", callbackUrl);
            try
            {
                string subject = "Confirm Your Email Address";
                string encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);
                string body = $"Welcome! Please confirm your account by <a href='{encodedUrl}'>clicking here</a>.\nThis link is valid for 24 hours.";
                _emailService.SendMail(_smtpSettings, userEmail, null, subject, body, true);
                _logger.LogInformation("API: Confirmation link email sent to {Email} for User {UserId}", userEmail, userId);
            }
            catch (Exception emailEx) { _logger.LogError(emailEx, "API: Failed to send confirmation email to {Email} for User {UserId}", userEmail, userId); }
        }

        private string GenerateSecureToken()
        {
            using (var rng = RandomNumberGenerator.Create()) { var tokenBytes = new byte[32]; rng.GetBytes(tokenBytes); return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('='); }
        }
    }
}
