using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Ensure controller requires login
using Project3.Models.ViewModels; // Needed for the ViewModel
using Project3.Utilities; // Assuming DBConnect is here
using System.Security.Claims; // Needed to get logged-in user ID
using System.Threading.Tasks; // *** ADDED for async Task ***
using System.Data;
// NOTE: Use EITHER System.Data.SqlClient OR Microsoft.Data.SqlClient, not both usually.
// Choose based on which package your DBConnect class uses.
using System.Data.SqlClient; // Assuming DBConnect uses this older version
// using Microsoft.Data.SqlClient; // Use this if DBConnect uses the newer package
using System;
using System.Collections.Generic; // For List<>
using Project3.Models.DTOs; // For ReviewDto etc.
using Microsoft.Extensions.Logging; // For logging
using System.Linq; // Add this for LINQ support
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using System.IO; // For Path

namespace Project3.Controllers
{
    [Authorize(Roles = "RestaurantRep")] // Restrict access to users with the RestaurantRep role
    public class RestaurantRepHomeController : Controller
    {
        private readonly DBConnect _db; // Example: Using DBConnect
        private readonly ILogger<RestaurantRepHomeController> _logger;

        // Constructor for dependency injection
        public RestaurantRepHomeController(DBConnect db, ILogger<RestaurantRepHomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /RestaurantRepHome/Index
        // FIX: Changed method signature to async Task<IActionResult>
        public async Task<IActionResult> Index()
        {
            var viewModel = new RestaurantRepHomeViewModel();
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID
            string username = User.Identity?.Name ?? "Restaurant Rep"; // Get username for welcome message

            viewModel.WelcomeMessage = $"Welcome, {username}!";

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found for RestaurantRepHome Index.");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // --- Fetch Profile Status ---
                SqlCommand cmdProfile = new SqlCommand();
                cmdProfile.CommandText = @"
                    SELECT 
                        CASE WHEN EXISTS (
                            SELECT 1 FROM TP_Restaurants WHERE RestaurantID = @UserId
                        ) THEN 1 ELSE 0 END as HasProfile,
                        r.RestaurantID,
                        r.Name as RestaurantName
                    FROM TP_Restaurants r
                    WHERE r.RestaurantID = @UserId";
                cmdProfile.Parameters.AddWithValue("@UserId", userId);
                
                DataSet dsProfile = _db.GetDataSetUsingCmdObj(cmdProfile);

                if (dsProfile.Tables.Count > 0 && dsProfile.Tables[0].Rows.Count > 0)
                {
                    DataRow profileRow = dsProfile.Tables[0].Rows[0];
                    viewModel.HasProfile = Convert.ToBoolean(profileRow["HasProfile"]);
                    if (viewModel.HasProfile)
                    {
                        viewModel.RestaurantId = Convert.ToInt32(profileRow["RestaurantId"]);
                        viewModel.RestaurantName = profileRow["RestaurantName"]?.ToString();
                    }
                }
                else
                {
                    viewModel.HasProfile = false;
                }


                // --- Get Pending Reservation Count (only if profile exists) ---
                if (viewModel.HasProfile)
                {
                    SqlCommand cmdReservations = new SqlCommand();
                    cmdReservations.CommandType = CommandType.StoredProcedure;
                    cmdReservations.CommandText = "TP_GetPendingReservationCount"; // Example SP name
                    cmdReservations.Parameters.AddWithValue("@RestaurantId", viewModel.RestaurantId);

                    // FIX: Changed to call the ASYNC method with await
                    // Using the correct method name from your DBConnect class
                    object reservationResult = await _db.ExecuteScalarUsingCmdObjAsync(cmdReservations); // <-- Corrected Call

                    if (reservationResult != null && reservationResult != DBNull.Value)
                    {
                        viewModel.PendingReservationCount = Convert.ToInt32(reservationResult);
                    }
                    else
                    {
                        viewModel.PendingReservationCount = 0; // Default if null or DBNull
                    }
                }


                // --- Get Recent Reviews (only if profile exists) ---
                if (viewModel.HasProfile)
                {
                    SqlCommand cmdReviews = new SqlCommand();
                    cmdReviews.CommandType = CommandType.StoredProcedure;
                    cmdReviews.CommandText = "TP_GetRecentReviewsForRestaurant"; // Example SP name
                    cmdReviews.Parameters.AddWithValue("@RestaurantId", viewModel.RestaurantId);
                    cmdReviews.Parameters.AddWithValue("@Count", 3); // Example: Get top 3 recent reviews
                                                                     // Assuming GetDataSetUsingCmdObj is SYNCHRONOUS
                    DataSet dsReviews = _db.GetDataSetUsingCmdObj(cmdReviews);

                    if (dsReviews.Tables.Count > 0 && dsReviews.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow row in dsReviews.Tables[0].Rows)
                        {
                            viewModel.RecentReviews.Add(new ReviewDto
                            {
                                ReviewId = Convert.ToInt32(row["ReviewId"]),
                                Rating = Convert.ToDecimal(row["Rating"]),
                                Comment = row["Comment"]?.ToString(),
                                ReviewDate = Convert.ToDateTime(row["ReviewDate"]),
                                ReviewerUsername = row["ReviewerUsername"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for RestaurantRepHome for User ID {UserId}", userId);
                ViewBag.ErrorMessage = "Could not load dashboard data.";
            }

            return View(viewModel); // Pass the populated ViewModel to the View
        }

        // GET: /RestaurantRepHome/ManageProfile
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ManageProfile()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get the restaurant profile if it exists
                var cmd = new SqlCommand("SELECT * FROM TP_Restaurants WHERE RestaurantID = @UserID");
                cmd.Parameters.AddWithValue("@UserID", userId);
                var ds = _db.GetDataSetUsingCmdObj(cmd);

                // Get available cuisines
                var cuisineCmd = new SqlCommand("SELECT DISTINCT Cuisine FROM TP_Restaurants WHERE Cuisine IS NOT NULL AND Cuisine != '' ORDER BY Cuisine");
                var cuisineDs = _db.GetDataSetUsingCmdObj(cuisineCmd);
                var cuisines = new List<string>();
                
                if (cuisineDs.Tables.Count > 0)
                {
                    foreach (DataRow row in cuisineDs.Tables[0].Rows)
                    {
                        cuisines.Add(row["Cuisine"].ToString());
                    }
                }

                // Add some common cuisines if none exist yet
                if (cuisines.Count == 0)
                {
                    cuisines.AddRange(new[] { "American", "Italian", "Mexican", "Chinese", "Japanese", "Indian", "Thai", "Mediterranean", "French", "Greek" });
                }

                var viewModel = new RestaurantViewModel();
                bool isNewProfile = true;

                if (ds?.Tables[0]?.Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    viewModel = new RestaurantViewModel
                    {
                        RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                        Name = row["Name"]?.ToString() ?? string.Empty,
                        Address = row["Address"]?.ToString() ?? string.Empty,
                        City = row["City"]?.ToString() ?? string.Empty,
                        State = row["State"]?.ToString() ?? string.Empty,
                        ZipCode = row["ZipCode"]?.ToString() ?? string.Empty,
                        Cuisine = row["Cuisine"]?.ToString() ?? string.Empty,
                        Hours = row["Hours"]?.ToString() ?? string.Empty,
                        Contact = row["Contact"]?.ToString() ?? string.Empty,
                        MarketingDescription = row["MarketingDescription"]?.ToString() ?? string.Empty,
                        WebsiteURL = row["WebsiteURL"]?.ToString() ?? string.Empty,
                        SocialMedia = row["SocialMedia"]?.ToString() ?? string.Empty,
                        Owner = row["Owner"]?.ToString() ?? string.Empty,
                        ProfilePhoto = row["ProfilePhoto"]?.ToString() ?? string.Empty,
                        LogoPhoto = row["LogoPhoto"]?.ToString() ?? string.Empty
                    };
                    isNewProfile = false;
                }
                else
                {
                    viewModel.RestaurantID = Convert.ToInt32(userId);
                }

                ViewData["IsNewProfile"] = isNewProfile;
                ViewData["Title"] = isNewProfile ? "Create Restaurant Profile" : "Update Restaurant Profile";
                ViewData["Cuisines"] = new SelectList(cuisines);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading restaurant profile");
                TempData["ErrorMessage"] = "An error occurred while loading the profile.";
                return RedirectToAction("Index");
            }
        }

        // POST: /RestaurantRepHome/ManageProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ManageProfile(RestaurantViewModel model)
        {
            _logger.LogInformation("Starting ManageProfile POST action");
            
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || userId != model.RestaurantID.ToString())
                {
                    _logger.LogWarning("User ID mismatch or not found");
                    return Forbid();
                }

                // Check if profile exists
                var checkCmd = new SqlCommand("SELECT 1 FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                checkCmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                var checkDs = _db.GetDataSetUsingCmdObj(checkCmd);
                bool isNewProfile = checkDs.Tables[0].Rows.Count == 0;

                _logger.LogInformation($"Is new profile: {isNewProfile}");

                // Log model state errors
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }

                    // Get available cuisines for the dropdown
                    var cuisineCmd = new SqlCommand("SELECT DISTINCT Cuisine FROM TP_Restaurants WHERE Cuisine IS NOT NULL AND Cuisine != '' ORDER BY Cuisine");
                    var cuisineDs = _db.GetDataSetUsingCmdObj(cuisineCmd);
                    var cuisines = new List<string>();
                    
                    if (cuisineDs.Tables.Count > 0)
                    {
                        foreach (DataRow row in cuisineDs.Tables[0].Rows)
                        {
                            cuisines.Add(row["Cuisine"].ToString());
                        }
                    }

                    if (cuisines.Count == 0)
                    {
                        cuisines.AddRange(new[] { "American", "Italian", "Mexican", "Chinese", "Japanese", "Indian", "Thai", "Mediterranean", "French", "Greek" });
                    }

                    ViewData["IsNewProfile"] = isNewProfile;
                    ViewData["Title"] = isNewProfile ? "Create Restaurant Profile" : "Update Restaurant Profile";
                    ViewData["Cuisines"] = new SelectList(cuisines);
                    return View(model);
                }

                _logger.LogInformation("Model state is valid, proceeding with save");

                // Handle file uploads
                if (model.ProfilePhotoFile != null && model.ProfilePhotoFile.Length > 0)
                {
                    _logger.LogInformation("Processing profile photo upload");
                    string fileName = $"{model.RestaurantID}_profile_{DateTime.Now.Ticks}{Path.GetExtension(model.ProfilePhotoFile.FileName)}";
                    string filePath = Path.Combine("wwwroot", "images", "restaurants", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.ProfilePhotoFile.CopyTo(stream);
                    }
                    
                    model.ProfilePhoto = $"/images/restaurants/{fileName}";
                }
                else
                {
                    // If no new file is uploaded, keep the existing photo or set to null
                    model.ProfilePhoto = model.ProfilePhoto ?? null;
                }

                if (model.LogoPhotoFile != null && model.LogoPhotoFile.Length > 0)
                {
                    _logger.LogInformation("Processing logo upload");
                    string fileName = $"{model.RestaurantID}_logo_{DateTime.Now.Ticks}{Path.GetExtension(model.LogoPhotoFile.FileName)}";
                    string filePath = Path.Combine("wwwroot", "images", "restaurants", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.LogoPhotoFile.CopyTo(stream);
                    }
                    
                    model.LogoPhoto = $"/images/restaurants/{fileName}";
                }
                else
                {
                    // If no new file is uploaded, keep the existing logo or set to null
                    model.LogoPhoto = model.LogoPhoto ?? null;
                }

                SqlCommand cmd;
                if (isNewProfile)
                {
                    _logger.LogInformation("Creating new restaurant profile");
                    cmd = new SqlCommand("INSERT INTO TP_Restaurants (RestaurantID, Name, Address, City, State, ZipCode, Cuisine, Hours, Contact, MarketingDescription, WebsiteURL, SocialMedia, Owner, ProfilePhoto, LogoPhoto) " +
                                       "VALUES (@RestaurantID, @Name, @Address, @City, @State, @ZipCode, @Cuisine, @Hours, @Contact, @MarketingDescription, @WebsiteURL, @SocialMedia, @Owner, @ProfilePhoto, @LogoPhoto)");
                }
                else
                {
                    _logger.LogInformation("Updating existing restaurant profile");
                    cmd = new SqlCommand("UPDATE TP_Restaurants SET Name = @Name, Address = @Address, City = @City, State = @State, ZipCode = @ZipCode, " +
                                       "Cuisine = @Cuisine, Hours = @Hours, Contact = @Contact, MarketingDescription = @MarketingDescription, " +
                                       "WebsiteURL = @WebsiteURL, SocialMedia = @SocialMedia, Owner = @Owner, ProfilePhoto = @ProfilePhoto, " +
                                       "LogoPhoto = @LogoPhoto WHERE RestaurantID = @RestaurantID");
                }

                cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Address", (object)model.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@City", (object)model.City ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@State", (object)model.State ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ZipCode", (object)model.ZipCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cuisine", (object)model.Cuisine ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hours", (object)model.Hours ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Contact", (object)model.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MarketingDescription", (object)model.MarketingDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WebsiteURL", (object)model.WebsiteURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SocialMedia", (object)model.SocialMedia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Owner", (object)model.Owner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object)model.ProfilePhoto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LogoPhoto", (object)model.LogoPhoto ?? DBNull.Value);

                _logger.LogInformation("Executing database command");
                int result = _db.DoUpdateUsingCmdObj(cmd);
                _logger.LogInformation($"Database command result: {result}");

                if (result > 0)
                {
                    _logger.LogInformation("Profile saved successfully");
                    TempData["SuccessMessage"] = isNewProfile 
                        ? "Restaurant profile created successfully!" 
                        : "Restaurant profile updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    _logger.LogWarning("Failed to save profile - no rows affected");
                    ModelState.AddModelError("", "Failed to save the profile. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving restaurant profile for ID {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError("", "An error occurred while saving the profile. Please try again.");
                return View(model);
            }
        }

        // --- Action for Manage Reservations button ---
        public IActionResult ManageReservations()
        {
            _logger.LogInformation("Redirecting to Manage Reservations page.");
            // TODO: Redirect to the actual reservation management page
            // Example: return RedirectToAction("Index", "ReservationManagement"); // Adjust as needed
            return RedirectToAction("Index"); // Placeholder
        }

        // Other actions for RestaurantRepHomeController if needed...
    }
}
