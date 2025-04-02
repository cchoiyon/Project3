using Microsoft.AspNetCore.Mvc;
using Project3.Models;    // Uses models with explicit properties
using Project3.Utilities; // My utilities
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions; // For email check
using System; // For Random, Guid, Exception
using System.Collections.Generic; // For List
using System.Threading.Tasks; // Needed for async Task
using System.Security.Claims; // Needed for ClaimsPrincipal, Claim
using Microsoft.AspNetCore.Authentication; // Needed for HttpContext.SignInAsync/SignOutAsync
using Microsoft.AspNetCore.Authorization; // Needed for [Authorize] attribute if used here
using BCrypt.Net; // *** For BCrypt hashing ***
using Microsoft.Extensions.Options;
using System.Text;


// Make sure Session is setup in Program.cs
// Make sure System.Data.SqlClient NuGet package is installed
// Make sure Authentication is setup in Program.cs
// *** Make sure BCrypt.Net-Next NuGet package is installed ***

namespace Project3.Controllers // Project 3 namespace
{
    public class AccountController : Controller
    {
        // Need DBConnect and Email utils
        private readonly DBConnect objDB = new DBConnect();
        private readonly Email objEmail = new Email(); // Instantiate Email utility
        private static Random rng = new Random(); // For selecting random question

        // *** Constructor for Dependency Injection (Example for SmtpSettings) ***
        // If you register DBConnect/Email for DI, inject them here too.
        private readonly SmtpSettings _smtpSettings;
        public AccountController(IOptions<SmtpSettings> smtpSettingsOptions)
        {
            _smtpSettings = smtpSettingsOptions.Value; // Get the settings object
        }
        // *** End Constructor ***


        // GET: /Account/Register
        public IActionResult Register()
        {
            var model = new RegisterModel();
            // model.UserType = "reviewer"; // Example default
            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model) // Model has explicit properties
        {
            // === Manual Validation ===
            ValidateRegisterModel(model); // My validation helper method

            // Check usrname unique if basic validation passed
            if (ModelState.IsValid)
            {
                try
                {
                    SqlCommand checkCmd = new SqlCommand();
                    checkCmd.CommandType = CommandType.StoredProcedure;
                    checkCmd.CommandText = "dbo.TP_spCheckUsernameExists"; // SP for checking username
                    checkCmd.Parameters.AddWithValue("@Username", model.Username);
                    DataSet dsCheck = objDB.GetDataSetUsingCmdObj(checkCmd);

                    // Check result from SP (1 if exists, 0 otherwise)
                    if (dsCheck != null && dsCheck.Tables.Count > 0 && dsCheck.Tables[0].Rows.Count > 0 && Convert.ToInt32(dsCheck.Tables[0].Rows[0]["DoesExist"]) > 0)
                    {
                        ModelState.AddModelError("Username", "Username already taken, pick another.");
                    }
                    // TODO: Check Email unique using TP_spCheckEmailExists?
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error checking username."); // Log ex?
                }
            }


            if (ModelState.IsValid) // Check again after unique check
            {
                try
                {
                    // Add User using Stored Procedure TP_spAddUser
                    SqlCommand addCmd = new SqlCommand();
                    addCmd.CommandType = CommandType.StoredProcedure;
                    addCmd.CommandText = "dbo.TP_spAddUser"; // SP to add user

                    addCmd.Parameters.AddWithValue("@Username", model.Username);
                    addCmd.Parameters.AddWithValue("@Email", model.Email);

                    // *** HASH THE PSSWORD using BCrypt ***
                    string hashedPassword = HashPassword(model.Password);
                    if (hashedPassword == null)
                    {
                        ViewData["ErrorMessage"] = "Error processing password during registration.";
                        return View(model);
                    }
                    addCmd.Parameters.AddWithValue("@UserPassword", hashedPassword); // Pass HASH to SP

                    addCmd.Parameters.AddWithValue("@UserType", model.UserType);

                    // Add parameters for HASHED Security Ansers using BCrypt
                    addCmd.Parameters.AddWithValue("@SecurityQuestion1", model.SecurityQuestion1);
                    addCmd.Parameters.AddWithValue("@SecurityAnswerHash1", HashAnswer(model.SecurityAnswer1)); // Uses BCrypt now
                    addCmd.Parameters.AddWithValue("@SecurityQuestion2", model.SecurityQuestion2);
                    addCmd.Parameters.AddWithValue("@SecurityAnswerHash2", HashAnswer(model.SecurityAnswer2)); // Uses BCrypt now
                    addCmd.Parameters.AddWithValue("@SecurityQuestion3", model.SecurityQuestion3);
                    addCmd.Parameters.AddWithValue("@SecurityAnswerHash3", HashAnswer(model.SecurityAnswer3)); // Uses BCrypt now

                    // Execute SP - get new user id back
                    DataSet dsResult = objDB.GetDataSetUsingCmdObj(addCmd);
                    int newUserId = -1;
                    if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                    {
                        if (dsResult.Tables[0].Columns.Contains("NewUserID"))
                        {
                            newUserId = Convert.ToInt32(dsResult.Tables[0].Rows[0]["NewUserID"]);
                        }
                        else
                        {
                            ViewData["ErrorMessage"] = "Registration failed (couldnt get user ID).";
                            return View(model);
                        }
                    }


                    if (newUserId > 0) // Check if insert worked
                    {
                        // === Two-Step Verifcation Logic ===
                        string verificationToken = Guid.NewGuid().ToString("N"); // Generate simple token
                        StoreVerificationTokenInDB(newUserId, verificationToken); // Store it
                        SendVerificationEmail(model.Email, newUserId, verificationToken); // Send email

                        TempData["Message"] = "Registration successful! Check your email (" + model.Email + ") to verify.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ViewData["ErrorMessage"] = "Registration failed (db error).";
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "Error during registration: " + ex.Message;
                    // Log ex details?
                    return View(model);
                }
            }
            // ModelState invalid, show form again
            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            var model = new LoginModel();
            // Check for cookie
            if (Request.Cookies["RememberedUsername"] != null)
            {
                model.Username = Request.Cookies["RememberedUsername"] ?? string.Empty;
                model.RememberMe = !string.IsNullOrEmpty(model.Username);
            }
            ViewData["Message"] = TempData["Message"]; // Show message from redirect
            return View(model);
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model) // Model has explicit properties
        {
            // Manual Validation
            if (string.IsNullOrWhiteSpace(model.Username))
                ModelState.AddModelError("Username", "Username required.");
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password required.");


            if (ModelState.IsValid)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.TP_spCheckUser"; // SP to check login

                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@UserPassword", model.Password); // Pass plain pssword - SP returns hash

                    DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow userData = ds.Tables[0].Rows[0];
                        string storedHash = userData["PasswordHash"]?.ToString();
                        bool isVerified = userData["IsVerified"] != DBNull.Value && Convert.ToBoolean(userData["IsVerified"]);
                        string userId = userData["UserID"].ToString();
                        string userType = userData["UserType"]?.ToString();

                        // *** Verify HASHED Pssword using BCrypt ***
                        if (!VerifyPassword(model.Password, storedHash)) // Uses BCrypt now
                        {
                            ViewData["ErrorMessage"] = "Invalid username or password.";
                            return View(model);
                        }

                        // *** MODIFIED: IsVerified check is commented out per user request ***
                        // if (!isVerified)
                        // {
                        //      ViewData["ErrorMessage"] = "Account not verified. Check email.";
                        //      return View(model);
                        // }
                        // *** END MODIFICATION ***

                        // --- Login Success ---

                        // Set Session (optional if using claims)
                        HttpContext.Session.SetString("UserID", userId);
                        HttpContext.Session.SetString("Username", model.Username);
                        HttpContext.Session.SetString("UserType", userType);

                        // *** Sign In for Cookie Authentication ***
                        var claims = new List<Claim> {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim("UserID", userId),
                            new Claim(ClaimTypes.Role, userType ?? "") // Use Role claim
                        };
                        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe
                        };
                        await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);
                        // *** END Sign In Block ***

                        // Handle Remember Login ID Cookie
                        if (model.RememberMe)
                        {
                            CookieOptions cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(30), HttpOnly = true, Secure = true, IsEssential = true };
                            Response.Cookies.Append("RememberedUsername", model.Username, cookieOptions);
                        }
                        else
                        {
                            Response.Cookies.Delete("RememberedUsername");
                        }

                        // Redirect based on UserType
                        if (userType.Equals("restaurantRep", StringComparison.OrdinalIgnoreCase))
                        {
                            return RedirectToAction("Index", "RestaurantRepHome"); // Redirect Reps
                        }
                        else if (userType.Equals("reviewer", StringComparison.OrdinalIgnoreCase))
                        {
                            return RedirectToAction("Index", "ReviewerHome"); // Redirect Reviewers
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Unknown user type encountered.";
                            await HttpContext.SignOutAsync("MyCookieAuth");
                            HttpContext.Session.Clear();
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else { ViewData["ErrorMessage"] = "Invalid username or password."; return View(model); }
                }
                catch (Exception ex) { ViewData["ErrorMessage"] = "Login error: " + ex.Message; return View(model); }
            }
            return View(model); // ModelState invalid
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RememberedUsername");
            TempData["Message"] = "Logged out.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/DeleteLoginCookie
        [HttpGet]
        public IActionResult DeleteLoginCookie()
        {
            Response.Cookies.Delete("RememberedUsername");
            TempData["Message"] = "Saved login cleared.";
            return RedirectToAction("Login");
        }


        // --- Password Reset & Username Recovery Actions ---

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword() { return View(new ForgotPasswordModel()); }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EmailOrUsername)) ModelState.AddModelError("EmailOrUsername", "Email or Username required.");
            if (ModelState.IsValid)
            {
                try
                {
                    // TODO: Lookup user using TP_spGetUserByUsernameOrEmail -> get UserID, Email
                    // If user found {
                    //    Generate token, expiry
                    //    TODO: Store token/expiry using TP_spStorePasswordResetToken
                    //    SendPasswordResetEmail(email, userId, resetToken); // <<< Use updated helper
                    // }
                    TempData["Message"] = "If account exists, password reset email sent.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex) { ViewData["ErrorMessage"] = "Error processing request."; return View(model); }
            }
            return View(model);
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string userId, string token)
        {
            // TODO: Validate userId and token using TP_spValidatePasswordResetToken
            bool isValidToken = true; // Placeholder
            if (isValidToken) { return View("ResetPassword", new ResetPasswordModel { UserId = userId, Token = token }); }
            else { TempData["ErrorMessage"] = "Invalid/expired reset link."; return RedirectToAction("Login"); }
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordModel model)
        {
            // Manual Validation
            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6) ModelState.AddModelError("NewPassword", "Password needs 6+ characters.");
            if (model.NewPassword != model.ConfirmPassword) ModelState.AddModelError("ConfirmPassword", "Passwords dont match.");
            if (string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.Token)) ModelState.AddModelError("", "Missing ID or Token.");

            if (ModelState.IsValid)
            {
                try
                {
                    // TODO: Re-validate token using TP_spValidatePasswordResetToken
                    bool isValid = true; // Placeholder
                    if (isValid)
                    {
                        // *** HASH THE NEW PSSWORD using BCrypt! ***
                        string hashedPassword = HashPassword(model.NewPassword);
                        // TODO: Update password using TP_spUpdateUserPassword(model.UserId, hashedPassword);
                        // TODO: Invalidate token using TP_spInvalidatePasswordResetToken(model.UserId);
                        TempData["Message"] = "Password reset success.";
                        return RedirectToAction("Login");
                    }
                    else { ViewData["ErrorMessage"] = "Invalid/expired reset link."; }
                }
                catch (Exception ex) { ViewData["ErrorMessage"] = "Error resetting pssword."; }
            }
            return View("ResetPassword", model);
        }

        // GET: /Account/ForgotUsername
        public IActionResult ForgotUsername() { return View(new ForgotUsernameModel()); }

        // POST: /Account/ForgotUsername
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotUsername(ForgotUsernameModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email)) ModelState.AddModelError("Email", "Email required.");
            else if (!IsValidEmail(model.Email)) ModelState.AddModelError("Email", "Enter valid email.");

            if (ModelState.IsValid)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.TP_spGetUserByEmail";
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    DataSet dsUsers = objDB.GetDataSetUsingCmdObj(cmd);
                    if (dsUsers?.Tables.Count > 0 && dsUsers.Tables[0].Rows.Count > 0)
                    {
                        string userId = dsUsers.Tables[0].Rows[0]["UserID"].ToString();
                        TempData["RecoverUserID"] = userId;
                        return RedirectToAction("AnswerSecurityQuestion");
                    }
                    else { TempData["Message"] = "If account exists, recovery started."; return RedirectToAction("Login"); }
                }
                catch (Exception ex) { ViewData["ErrorMessage"] = "Error finding user."; return View(model); }
            }
            return View(model);
        }

        // GET: /Account/AnswerSecurityQuestion
        public IActionResult AnswerSecurityQuestion()
        {
            if (TempData["RecoverUserID"] == null) { TempData["ErrorMessage"] = "User ID missing."; return RedirectToAction("ForgotUsername"); }
            string userIdStr = TempData["RecoverUserID"].ToString();
            TempData.Keep("RecoverUserID");
            if (!int.TryParse(userIdStr, out int userId)) { TempData["ErrorMessage"] = "Invalid user ID."; return RedirectToAction("ForgotUsername"); }

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spGetUserSecurityQuestions";
                cmd.Parameters.AddWithValue("@UserID", userId);
                DataSet dsQuestions = objDB.GetDataSetUsingCmdObj(cmd);
                if (dsQuestions?.Tables.Count > 0 && dsQuestions.Tables[0].Rows.Count > 0)
                {
                    DataRow questions = dsQuestions.Tables[0].Rows[0];
                    var questionList = new List<KeyValuePair<int, string>>();
                    if (questions["SecurityQuestion1"] != DBNull.Value && !string.IsNullOrEmpty(questions["SecurityQuestion1"].ToString())) questionList.Add(new KeyValuePair<int, string>(1, questions["SecurityQuestion1"].ToString()));
                    if (questions["SecurityQuestion2"] != DBNull.Value && !string.IsNullOrEmpty(questions["SecurityQuestion2"].ToString())) questionList.Add(new KeyValuePair<int, string>(2, questions["SecurityQuestion2"].ToString()));
                    if (questions["SecurityQuestion3"] != DBNull.Value && !string.IsNullOrEmpty(questions["SecurityQuestion3"].ToString())) questionList.Add(new KeyValuePair<int, string>(3, questions["SecurityQuestion3"].ToString()));
                    if (questionList.Count == 0) { TempData["ErrorMessage"] = "No security questions found."; return RedirectToAction("Login"); }

                    var selectedQuestion = questionList[rng.Next(questionList.Count)];
                    var viewModel = new AnswerSecurityQuestionModel { UserId = userIdStr, QuestionNumber = selectedQuestion.Key, QuestionText = selectedQuestion.Value };
                    return View("AnswerSecurityQuestion", viewModel);
                }
                else { TempData["ErrorMessage"] = "Could not get security questions."; return RedirectToAction("Login"); }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Error getting security question."; return RedirectToAction("Login"); }
        }

        // POST: /Account/AnswerSecurityQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AnswerSecurityQuestion(AnswerSecurityQuestionModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(viewModel.Answer)) ModelState.AddModelError("Answer", "Please provide an answer.");
            if (string.IsNullOrWhiteSpace(viewModel.UserId) || viewModel.QuestionNumber <= 0 || viewModel.QuestionNumber > 3) ModelState.AddModelError("", "Invalid request data.");
            TempData.Keep("RecoverUserID");

            if (ModelState.IsValid)
            {
                try
                {
                    // *** Hash the provided answer using BCrypt ***
                    string answerHash = HashAnswer(viewModel.Answer);

                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.TP_spValidateSecurityAnswer";
                    cmd.Parameters.AddWithValue("@UserID", Convert.ToInt32(viewModel.UserId));
                    cmd.Parameters.AddWithValue("@QuestionNumber", viewModel.QuestionNumber);
                    cmd.Parameters.AddWithValue("@AnswerToCheckHash", answerHash);
                    DataSet dsResult = objDB.GetDataSetUsingCmdObj(cmd);

                    if (dsResult?.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0 && Convert.ToInt32(dsResult.Tables[0].Rows[0]["IsCorrect"]) == 1)
                    {
                        // Correct Answer - Get Username
                        SqlCommand getUserCmd = new SqlCommand();
                        getUserCmd.CommandType = CommandType.Text; // Or use TP_spGetUserById
                        getUserCmd.CommandText = "SELECT Username FROM dbo.TP_Users WHERE UserID = @UserID";
                        getUserCmd.Parameters.AddWithValue("@UserID", Convert.ToInt32(viewModel.UserId));
                        DataSet dsUser = objDB.GetDataSetUsingCmdObj(getUserCmd);
                        if (dsUser?.Tables.Count > 0 && dsUser.Tables[0].Rows.Count > 0)
                        {
                            string username = dsUser.Tables[0].Rows[0]["Username"].ToString();
                            TempData["Message"] = $"Username recovery successful. Your username is: {username}";
                            TempData.Remove("RecoverUserID");
                            return RedirectToAction("Login");
                        }
                        else { ViewData["ErrorMessage"] = "Could not retrieve username."; }
                    }
                    else { ModelState.AddModelError("Answer", "Incorrect answer. Please try again."); }
                }
                catch (Exception ex) { ViewData["ErrorMessage"] = "Error validating answer."; }
            }

            // Re-fetch question text if returning view
            viewModel.QuestionText = GetQuestionText(viewModel.UserId, viewModel.QuestionNumber);
            if (string.IsNullOrEmpty(viewModel.QuestionText)) { TempData["ErrorMessage"] = "Error redisplaying question."; return RedirectToAction("ForgotUsername"); }
            return View("AnswerSecurityQuestion", viewModel);
        }


        // GET: /Account/VerifyEmail
        public IActionResult VerifyEmail(string userId, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token) || !int.TryParse(userId, out int userIdInt))
                { TempData["ErrorMessage"] = "Invalid verification link parameters."; return RedirectToAction("Login"); }

                SqlCommand cmdVal = new SqlCommand();
                cmdVal.CommandType = CommandType.StoredProcedure;
                cmdVal.CommandText = "dbo.TP_spValidateVerificationToken";
                cmdVal.Parameters.AddWithValue("@UserID", userIdInt);
                cmdVal.Parameters.AddWithValue("@VerificationToken", token);
                DataSet dsVal = objDB.GetDataSetUsingCmdObj(cmdVal);

                bool isValid = false;
                if (dsVal?.Tables.Count > 0 && dsVal.Tables[0].Rows.Count > 0 && dsVal.Tables[0].Columns.Contains("IsValid"))
                { isValid = Convert.ToInt32(dsVal.Tables[0].Rows[0]["IsValid"]) == 1; }

                if (isValid)
                {
                    SqlCommand cmdSet = new SqlCommand();
                    cmdSet.CommandType = CommandType.StoredProcedure;
                    cmdSet.CommandText = "dbo.TP_spSetUserVerified";
                    cmdSet.Parameters.AddWithValue("@UserID", userIdInt);
                    objDB.DoUpdateUsingCmdObj(cmdSet);
                    TempData["Message"] = "Email verified successfully! You can now log in.";
                }
                else { TempData["ErrorMessage"] = "Invalid or expired verification link."; }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "An error occurred during verification."; }
            return RedirectToAction("Login");
        }


        // --- Helper Methods ---

        // *** UPDATED to use BCrypt.Net ***
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;
            try { return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12)); }
            catch (Exception ex) { Console.WriteLine($"Error hashing password: {ex.Message}"); return null; }
        }

        // *** UPDATED to use BCrypt.Net ***
        private bool VerifyPassword(string providedPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(providedPassword) || string.IsNullOrEmpty(storedHash)) return false;
            try { return BCrypt.Net.BCrypt.Verify(providedPassword, storedHash); }
            catch (Exception ex) { Console.WriteLine($"Error verifying password: {ex.Message}"); return false; } // Catch potential exceptions from Verify
        }


        // *** UPDATED to use BCrypt.Net ***
        private string HashAnswer(string answer)
        {
            if (string.IsNullOrEmpty(answer)) return null;
            try { return BCrypt.Net.BCrypt.HashPassword(answer, BCrypt.Net.BCrypt.GenerateSalt(12)); } // Use same salt factor as password?
            catch (Exception ex) { Console.WriteLine($"Error hashing answer: {ex.Message}"); return null; }
        }


        // Manual Validation Helper for Registration
        private void ValidateRegisterModel(RegisterModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username)) ModelState.AddModelError("Username", "Username required.");
            if (string.IsNullOrWhiteSpace(model.Email)) ModelState.AddModelError("Email", "Email required.");
            if (string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError("Password", "Password required.");
            if (string.IsNullOrWhiteSpace(model.ConfirmPassword)) ModelState.AddModelError("ConfirmPassword", "Confirm Password required.");
            if (string.IsNullOrWhiteSpace(model.UserType)) ModelState.AddModelError("UserType", "Select account type.");
            if (string.IsNullOrWhiteSpace(model.SecurityQuestion1)) ModelState.AddModelError("SecurityQuestion1", "Select Question 1.");
            if (string.IsNullOrWhiteSpace(model.SecurityAnswer1)) ModelState.AddModelError("SecurityAnswer1", "Answer Question 1.");
            if (string.IsNullOrWhiteSpace(model.SecurityQuestion2)) ModelState.AddModelError("SecurityQuestion2", "Select Question 2.");
            if (string.IsNullOrWhiteSpace(model.SecurityAnswer2)) ModelState.AddModelError("SecurityAnswer2", "Answer Question 2.");
            if (string.IsNullOrWhiteSpace(model.SecurityQuestion3)) ModelState.AddModelError("SecurityQuestion3", "Select Question 3.");
            if (string.IsNullOrWhiteSpace(model.SecurityAnswer3)) ModelState.AddModelError("SecurityAnswer3", "Answer Question 3.");

            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email)) ModelState.AddModelError("Email", "Enter valid email address.");
            if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 6) ModelState.AddModelError("Password", "Password needs 6+ characters.");
            if (model.Password != model.ConfirmPassword) ModelState.AddModelError("ConfirmPassword", "Passwords dont match.");
            // TODO: Check if security questions are unique?
        }

        // Simple Email Validation Helper
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)); }
            catch (RegexMatchTimeoutException) { return false; }
        }

        // *** UPDATED SendVerificationEmail using P2 logic pattern ***
        private void SendVerificationEmail(string recipientEmail, int userId, string token)
        {
            string fromAddress = _smtpSettings.FromAddress ?? "tuo53004@cis-linux2.temple.edu"; // Use setting
            string subject = "Verify Your Account";
            string verificationUrl = Url.Action("VerifyEmail", "Account", new { userId = userId, token = token }, Request.Scheme);

            if (string.IsNullOrEmpty(verificationUrl)) { Console.WriteLine($"ERROR: Could not generate verification URL for user {userId}. Email not sent."); return; }

            StringBuilder body = new StringBuilder();
            body.Append("<html><body><h2>Welcome! Please Verify Your Email</h2>");
            body.Append($"<p>Thank you for registering. Please click the link below to verify your email address:</p>");
            body.Append($"<p><a href='{verificationUrl}'>Verify My Email Address</a></p><hr>");
            body.Append("<p>If you did not register, please ignore this email.</p></body></html>");

            try
            {
                Email emailUtility = new Email(); // Create instance here
                emailUtility.SendMail(_smtpSettings, recipientEmail, fromAddress, subject, body.ToString(), true); // Pass settings
                Console.WriteLine($"Verification email sent to {recipientEmail}");
            }
            catch (Exception ex) { Console.WriteLine($"ERROR sending verification email to {recipientEmail}: {ex.ToString()}"); } // Log full exception
        }

        // *** UPDATED SendPasswordResetEmail using P2 logic pattern ***
        private void SendPasswordResetEmail(string recipientEmail, int userId, string token)
        {
            string fromAddress = _smtpSettings.FromAddress ?? "noreply@MyReviewSite.com"; // Use setting
            string subject = "Reset Your Password";
            string resetUrl = Url.Action("ResetPassword", "Account", new { userId = userId, token = token }, Request.Scheme);

            if (string.IsNullOrEmpty(resetUrl)) { Console.WriteLine($"ERROR: Could not generate reset URL for user {userId}. Email not sent."); return; }

            StringBuilder body = new StringBuilder();
            body.Append("<html><body><h2>Password Reset Request</h2>");
            body.Append($"<p>You requested a password reset. Click the following link to set a new password:</p>");
            body.Append($"<p><a href='{resetUrl}'>Reset Password</a></p><hr>");
            body.Append("<p>This link will expire (e.g., in 1 hour - implement expiry logic!).</p>");
            body.Append("<p>If you did not request this, please ignore this email.</p></body></html>");

            try
            {
                Email emailUtility = new Email(); // Create instance here
                emailUtility.SendMail(_smtpSettings, recipientEmail, fromAddress, subject, body.ToString(), true); // Pass settings
                Console.WriteLine($"Password reset email sent to {recipientEmail}");
            }
            catch (Exception ex) { Console.WriteLine($"ERROR sending password reset email to {recipientEmail}: {ex.ToString()}"); } // Log full exception
        }

        // Helper method to re-fetch question text
        private string GetQuestionText(string userIdStr, int questionNumber)
        {
            if (!int.TryParse(userIdStr, out int userId) || questionNumber <= 0 || questionNumber > 3) return null;
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spGetUserSecurityQuestions";
                cmd.Parameters.AddWithValue("@UserID", userId);
                DataSet dsQuestions = objDB.GetDataSetUsingCmdObj(cmd);
                if (dsQuestions?.Tables.Count > 0 && dsQuestions.Tables[0].Rows.Count > 0)
                {
                    DataRow questions = dsQuestions.Tables[0].Rows[0];
                    string columnName = $"SecurityQuestion{questionNumber}";
                    if (questions.Table.Columns.Contains(columnName) && questions[columnName] != DBNull.Value)
                    { return questions[columnName].ToString(); }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error re-fetching question {questionNumber} for user {userId}: {ex.Message}"); }
            return null;
        }

        // Helper method to store verification token
        private void StoreVerificationTokenInDB(int userId, string token)
        {
            try
            {
                // TODO: Create and call TP_spStoreVerificationToken SP or integrate into TP_spAddUser/TP_spUpdateUser
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spStoreVerificationToken"; // Assumed SP name
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@VerificationToken", token);
                // Use DoUpdateUsingCmdObj for non-query SPs that don't return DataSet
                int result = objDB.DoUpdateUsingCmdObj(cmd);
                if (result >= 0)
                { // Check result if DoUpdate returns rows affected or similar
                    Console.WriteLine($"Stored token {token} for user {userId}");
                }
                else
                {
                    Console.WriteLine($"Error storing verification token for user {userId} (DB operation failed).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing verification token for user {userId}: {ex.Message}"); // Log error
            }
        }

    }
}
