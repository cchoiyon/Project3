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
    [Authorize] // Require login for all review actions
    public class ReviewController : Controller
    {
        private readonly DBConnect objDB = new DBConnect();

        // GET: /Review/Index (Shows reviews by the logged-in user)
        public IActionResult Index()
        {
            string userIdStr = User.FindFirstValue("UserID"); // Get UserID claim
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account"); // Not logged in or invalid ID
            }

            List<ReviewViewModel> myReviews = new List<ReviewViewModel>();
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spGetReviewsByUser"; // Use TP_ prefix
                cmd.Parameters.AddWithValue("@UserID", userId);
                DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        myReviews.Add(new ReviewViewModel(dr));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user reviews: {ex.Message}"); // Log error
                ViewData["ErrorMessage"] = "Could not load your reviews.";
            }
            return View(myReviews); // Needs Views/Review/Index.cshtml
        }

        // GET: /Review/Create?restaurantId=123
        [Authorize(Roles = "reviewer")] // Only reviewers can create
        public IActionResult Create(int restaurantId)
        {
            if (restaurantId <= 0) { return BadRequest("Invalid Restaurant ID."); }
            ViewData["RestaurantName"] = GetRestaurantName(restaurantId) ?? "Selected Restaurant";
            Review model = new Review();
            model.RestaurantID = restaurantId;
            model.VisitDate = DateTime.Today;
            return View(model); // Needs Views/Review/Create.cshtml
        }

        // POST: /Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "reviewer")]
        public IActionResult Create(Review model)
        {
            // Manual Validation
            if (model.FoodQualityRating < 1 || model.FoodQualityRating > 5) ModelState.AddModelError("FoodQualityRating", "Rating must be 1-5.");
            if (model.ServiceRating < 1 || model.ServiceRating > 5) ModelState.AddModelError("ServiceRating", "Rating must be 1-5.");
            if (model.AtmosphereRating < 1 || model.AtmosphereRating > 5) ModelState.AddModelError("AtmosphereRating", "Rating must be 1-5.");
            if (model.PriceRating < 1 || model.PriceRating > 5) ModelState.AddModelError("PriceRating", "Rating must be 1-5.");
            if (model.VisitDate > DateTime.Today) ModelState.AddModelError("VisitDate", "Visit date cannot be in the future.");
            if (string.IsNullOrWhiteSpace(model.Comments)) ModelState.AddModelError("Comments", "Comments are required.");

            // *** FIX for CS0165: Declare userId outside the TryParse ***
            int userId; // Declare here
            string userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out userId)) // Assign here
            {
                // This should ideally not happen if [Authorize] is working, but handle defensively
                ModelState.AddModelError("", "User not identified. Please log in again.");
            }
            // *** END FIX ***

            if (ModelState.IsValid) // Check includes the userId check above now
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.TP_spAddReview"; // Use TP_ prefix
                    cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                    cmd.Parameters.AddWithValue("@UserID", userId); // Use userId declared above
                    cmd.Parameters.AddWithValue("@VisitDate", model.VisitDate);
                    cmd.Parameters.AddWithValue("@Comments", model.Comments);
                    cmd.Parameters.AddWithValue("@FoodQualityRating", model.FoodQualityRating);
                    cmd.Parameters.AddWithValue("@ServiceRating", model.ServiceRating);
                    cmd.Parameters.AddWithValue("@AtmosphereRating", model.AtmosphereRating);
                    cmd.Parameters.AddWithValue("@PriceRating", model.PriceRating);

                    int result = objDB.DoUpdateUsingCmdObj(cmd);

                    if (result > 0)
                    {
                        TempData["Message"] = "Review added successfully!";
                        return RedirectToAction("Index"); // Redirect to user's reviews list
                    }
                    else
                    {
                        ViewData["ErrorMessage"] = "Failed to add review.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding review: {ex.Message}"); // Log error
                    ViewData["ErrorMessage"] = "Error adding review.";
                }
            }

            // If failed, return view with errors
            ViewData["RestaurantName"] = GetRestaurantName(model.RestaurantID) ?? "Selected Restaurant";
            return View(model); // Show Create form again
        }


        // GET: /Review/Edit/5
        [Authorize(Roles = "reviewer")]
        public IActionResult Edit(int id) // id is ReviewID
        {
            string userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int currentUserId)) { return Unauthorized(); }

            ReviewViewModel model = GetReview(id);
            if (model == null) { return NotFound(); }
            if (model.UserID != currentUserId) { TempData["ErrorMessage"] = "You can only edit your own reviews."; return RedirectToAction("Index"); }

            ViewData["RestaurantName"] = model.RestaurantName;
            Review editModel = new Review
            {
                ReviewID = model.ReviewID,
                RestaurantID = model.RestaurantID,
                UserID = model.UserID,
                VisitDate = model.VisitDate,
                Comments = model.Comments,
                FoodQualityRating = model.FoodQualityRating,
                ServiceRating = model.ServiceRating,
                AtmosphereRating = model.AtmosphereRating,
                PriceRating = model.PriceRating
            };
            return View(editModel); // Needs Views/Review/Edit.cshtml
        }

        // POST: /Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "reviewer")]
        public IActionResult Edit(int id, Review model) // id from route, model from form
        {
            if (id != model.ReviewID) { return BadRequest(); }

            // Manual Validation (similar to Create)
            if (model.FoodQualityRating < 1 || model.FoodQualityRating > 5) ModelState.AddModelError("FoodQualityRating", "Rating must be 1-5.");
            // ... add other validation checks ...
            if (string.IsNullOrWhiteSpace(model.Comments)) ModelState.AddModelError("Comments", "Comments are required.");


            int currentUserId = 0; // Declare here
            string userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out currentUserId))
            { ModelState.AddModelError("", "User not identified."); }
            else
            {
                // Security Check: Verify user owns the review *before* updating
                ReviewViewModel existingReview = GetReview(id);
                if (existingReview == null || existingReview.UserID != currentUserId)
                { return NotFound(); } // Or RedirectToAction("Index") with error
            }


            if (ModelState.IsValid)
            {
                try
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "dbo.TP_spUpdateReview"; // Use TP_ prefix
                    cmd.Parameters.AddWithValue("@ReviewID", model.ReviewID);
                    cmd.Parameters.AddWithValue("@VisitDate", model.VisitDate);
                    cmd.Parameters.AddWithValue("@Comments", model.Comments);
                    cmd.Parameters.AddWithValue("@FoodQualityRating", model.FoodQualityRating);
                    cmd.Parameters.AddWithValue("@ServiceRating", model.ServiceRating);
                    cmd.Parameters.AddWithValue("@AtmosphereRating", model.AtmosphereRating);
                    cmd.Parameters.AddWithValue("@PriceRating", model.PriceRating);

                    int result = objDB.DoUpdateUsingCmdObj(cmd);

                    if (result > 0) { TempData["Message"] = "Review updated successfully!"; return RedirectToAction("Index"); }
                    else { ViewData["ErrorMessage"] = "Failed to update review."; }
                }
                catch (Exception ex) { Console.WriteLine($"Error updating review {id}: {ex.Message}"); ViewData["ErrorMessage"] = "Error updating review."; }
            }

            // If failed, return view with errors
            ViewData["RestaurantName"] = GetRestaurantName(model.RestaurantID) ?? "Selected Restaurant";
            return View(model); // Show Edit form again
        }


        // GET: /Review/Delete/5
        [Authorize(Roles = "reviewer")]
        public IActionResult Delete(int id) // id is ReviewID
        {
            string userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int currentUserId)) { return Unauthorized(); }

            ReviewViewModel model = GetReview(id);
            if (model == null) { return NotFound(); }
            if (model.UserID != currentUserId) { TempData["ErrorMessage"] = "You can only delete your own reviews."; return RedirectToAction("Index"); }

            return View(model); // Pass ReviewViewModel to Views/Review/Delete.cshtml
        }


        // POST: /Review/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "reviewer")]
        public IActionResult DeleteConfirmed(int id) // id is ReviewID from route/form
        {
            string userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int currentUserId)) { return Unauthorized(); }

            try
            {
                // Security Check (Again): Verify user owns the review *before* deleting
                ReviewViewModel existingReview = GetReview(id);
                if (existingReview == null || existingReview.UserID != currentUserId) { return NotFound(); }

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spDeleteReview"; // Use TP_ prefix
                cmd.Parameters.AddWithValue("@ReviewID", id);

                int result = objDB.DoUpdateUsingCmdObj(cmd);

                if (result > 0) { TempData["Message"] = "Review deleted successfully."; }
                else { TempData["ErrorMessage"] = "Failed to delete review."; }
            }
            catch (Exception ex) { Console.WriteLine($"Error deleting review {id}: {ex.Message}"); TempData["ErrorMessage"] = "Error deleting review."; }

            return RedirectToAction("Index"); // Redirect back to the list of reviews
        }


        // --- Helper Methods ---

        // Helper to get a single review (used by Edit/Delete)
        private ReviewViewModel GetReview(int reviewId)
        {
            ReviewViewModel review = null;
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spGetReviewById"; // Use TP_ prefix
                cmd.Parameters.AddWithValue("@ReviewID", reviewId);
                DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) { review = new ReviewViewModel(ds.Tables[0].Rows[0]); }
            }
            catch (Exception ex) { Console.WriteLine($"Error getting review {reviewId}: {ex.Message}"); }
            return review;
        }

        // Helper to get restaurant name (used by Create/Edit)
        private string GetRestaurantName(int restaurantId)
        {
            string name = null;
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text; // Or use SP TP_spGetRestaurantNameById
                cmd.CommandText = "SELECT Name FROM dbo.TP_Restaurants WHERE RestaurantID = @RestaurantID";
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0) { name = ds.Tables[0].Rows[0]["Name"]?.ToString(); }
            }
            catch (Exception ex) { Console.WriteLine($"Error getting restaurant name {restaurantId}: {ex.Message}"); }
            return name;
        }

    }
}
