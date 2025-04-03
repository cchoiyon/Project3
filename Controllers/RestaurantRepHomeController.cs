using Microsoft.AspNetCore.Mvc;
using Project3.Models;
using Project3.Utilities;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System;

namespace Project3.Controllers
{
    [Authorize(Roles = "restaurantRep")] // Only allow Restaurant Reps
    public class RestaurantRepHomeController : Controller
    {
        private readonly DBConnect objDB = new DBConnect();

        // GET: /RestaurantRepHome/Index
        public IActionResult Index()
        {
            // Get the logged-in user's ID (which is the RestaurantID for reps)
            string userIdStr = User.FindFirstValue("UserID"); // Get UserID claim set during login
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int restaurantId))
            {
                // Handle error - user ID not found or invalid
                // Maybe redirect to login or show error message
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new RestaurantRepHomeViewModel();

            try
            {
                // Get Restaurant Profile
                viewModel.RestaurantProfile = GetRestaurantProfile(restaurantId);

                // Get Pending Reservations
                viewModel.PendingReservations = GetReservations(restaurantId, "Pending");

                // Get Recent Reviews
                viewModel.RecentReviews = GetReviews(restaurantId, 5); // Get latest 5
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading rep dashboard: {ex.Message}"); // Log error
                ViewData["ErrorMessage"] = "Error loading dashboard data.";
                // Still return view but maybe with partial data or just error
            }

            ViewData["Username"] = User.Identity.Name ?? "Representative";
            return View(viewModel); // Pass data to Views/RestaurantRepHome/Index.cshtml
        }

        // Helper to get profile
        private Restaurant GetRestaurantProfile(int restaurantId)
        {
            Restaurant profile = null;
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.TP_spGetRestaurantProfile";
            cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
            DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

            if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                // Map DataRow to Restaurant model (explicit properties)
                DataRow dr = ds.Tables[0].Rows[0];
                profile = new Restaurant();
                profile.RestaurantID = Convert.ToInt32(dr["RestaurantID"]);
                profile.Name = dr["Name"]?.ToString();
                profile.Address = dr["Address"]?.ToString();
                profile.City = dr["City"]?.ToString();
                profile.State = dr["State"]?.ToString();
                profile.ZipCode = dr["ZipCode"]?.ToString();
                profile.Cuisine = dr["Cuisine"]?.ToString();
                profile.Hours = dr["Hours"]?.ToString();
                profile.Contact = dr["Contact"]?.ToString();
                profile.ProfilePhoto = dr["ProfilePhoto"]?.ToString();
                profile.LogoPhoto = dr["LogoPhoto"]?.ToString();
                profile.MarketingDescription = dr["MarketingDescription"]?.ToString();
                profile.WebsiteURL = dr["WebsiteURL"]?.ToString();
                profile.SocialMedia = dr["SocialMedia"]?.ToString();
                profile.Owner = dr["Owner"]?.ToString();
                profile.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
            }
            return profile ?? new Restaurant(); // Return empty profile if not found
        }

        // Helper to get reservations (can be reused by ReservationManager)
        // Adding status parameter for flexibility
        private List<Reservation> GetReservations(int restaurantId, string statusFilter = null)
        {
            List<Reservation> reservations = new List<Reservation>();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            // Decide which SP to call based on filter
            if (statusFilter == "Pending")
            {
                cmd.CommandText = "dbo.TP_spGetPendingReservations";
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                // cmd.Parameters.AddWithValue("@MaxCount", 10); // If SP takes count
            }
            else
            {
                // TODO: Create/use a general SP like TP_spGetReservationsByRestaurant
                // cmd.CommandText = "dbo.TP_spGetReservationsByRestaurant";
                // cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                // if (statusFilter != null) cmd.Parameters.AddWithValue("@Status", statusFilter);
                return reservations; // Return empty for now if other SP not ready
            }

            DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);
            if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Reservation res = new Reservation();
                    res.ReservationID = Convert.ToInt32(dr["ReservationID"]);
                    res.RestaurantID = Convert.ToInt32(dr["RestaurantID"]);
                    res.UserID = dr["UserID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["UserID"]);
                    res.ReservationDateTime = Convert.ToDateTime(dr["ReservationDateTime"]);
                    res.PartySize = Convert.ToInt32(dr["PartySize"]);
                    res.ContactName = dr["ContactName"]?.ToString();
                    res.Phone = dr["Phone"]?.ToString();
                    res.Email = dr["Email"]?.ToString();
                    res.SpecialRequests = dr["SpecialRequests"]?.ToString();
                    res.Status = dr["Status"]?.ToString();
                    res.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                    reservations.Add(res);
                }
            }
            return reservations;
        }

        // Helper to get reviews (can be reused)
        private List<Review> GetReviews(int restaurantId, int maxCount = 0)
        {
            List<Review> reviews = new List<Review>();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.TP_spGetRecentReviews"; // Or a general SP?
            cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
            if (maxCount > 0) cmd.Parameters.AddWithValue("@MaxCount", maxCount);

            DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);
            if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Review review = new Review();
                    review.ReviewID = Convert.ToInt32(dr["ReviewID"]);
                    review.RestaurantID = Convert.ToInt32(dr["RestaurantID"]);
                    review.UserID = Convert.ToInt32(dr["UserID"]);
                    // reviewerUsername = dr["ReviewerUsername"]?.ToString(); // Available from SP, but Review model doesn't have it
                    review.VisitDate = Convert.ToDateTime(dr["VisitDate"]);
                    review.Comments = dr["Comments"]?.ToString();
                    review.FoodQualityRating = Convert.ToInt32(dr["FoodQualityRating"]);
                    review.ServiceRating = Convert.ToInt32(dr["ServiceRating"]);
                    review.AtmosphereRating = Convert.ToInt32(dr["AtmosphereRating"]);
                    review.PriceRating = Convert.ToInt32(dr["PriceRating"]);
                    review.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                    reviews.Add(review);
                }
            }
            return reviews;
        }

    }
}
