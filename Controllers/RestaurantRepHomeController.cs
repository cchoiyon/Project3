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
                cmdProfile.CommandType = CommandType.StoredProcedure;
                cmdProfile.CommandText = "TP_GetRestaurantProfileStatusByUserId"; // Example SP name
                cmdProfile.Parameters.AddWithValue("@UserId", userId);
                // Assuming GetDataSetUsingCmdObj exists and is correct in your DBConnect class
                // NOTE: If GetDataSetUsingCmdObj is also async, this Index method needs more async/await changes.
                // Assuming GetDataSetUsingCmdObj is SYNCHRONOUS for now based on your DBConnect code.
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

        // --- Action for Create Profile button ---
        public IActionResult CreateProfile()
        {
            _logger.LogInformation("Redirecting to Create/Edit Profile page.");
            // TODO: Redirect to the actual profile creation/edit page
            // Example: return RedirectToAction("Create", "RestaurantProfile"); // Adjust as needed
            return RedirectToAction("Index"); // Placeholder
        }

        // --- Action for Manage Profile button ---
        public IActionResult ManageProfile()
        {
            _logger.LogInformation("Redirecting to Manage Profile/Photos page.");
            // TODO: Redirect to the actual profile management page
            // Example: return RedirectToAction("Edit", "RestaurantProfile", new { id = viewModel.RestaurantId }); // Pass ID if needed from model state
            return RedirectToAction("Index"); // Placeholder
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
