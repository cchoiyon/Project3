using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project3.Models.Domain; // Assuming domain models are here
using Project3.Models.ViewModels; // Assuming ViewModels are used as return types
// using Project3.Models.DTOs; // Add using for your DTOs
using Project3.Utilities; // For DBConnect
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims; // For getting UserID
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // For authorization attributes
using Project3.Models.InputModels;

namespace Project3.Controllers.API // Or just Project3.Controllers
{
    [Route("api/[controller]")] // Base route: api/reviews
    [ApiController]
    public class ReviewsApiController : ControllerBase
    {
        private readonly ILogger<ReviewsApiController> _logger;
        private readonly DBConnect _dbConnect; // Inject DBConnect (consider repository pattern later)

        public ReviewsApiController(ILogger<ReviewsApiController> logger, DBConnect dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        // POST: api/reviews
        [HttpPost]
        [Authorize(Roles = "reviewer")] // Only reviewers can add reviews
        public async Task<ActionResult<Review>> AddReview([FromBody] CreateReviewDto reviewDto) // Assuming CreateReviewDto exists
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not identified.");
            }

            _logger.LogInformation("API: Attempting to add review for Restaurant {RestaurantId} by User {UserId}", reviewDto.RestaurantID, userId);

            if (!ModelState.IsValid) // Check DTO validation attributes
            {
                return BadRequest(ModelState);
            }

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spAddReview";

                // TODO: Add parameters from reviewDto, ensuring UserID comes from authenticated user
                cmd.Parameters.AddWithValue("@RestaurantID", reviewDto.RestaurantID);
                cmd.Parameters.AddWithValue("@UserID", userId); // Use authenticated user ID
                cmd.Parameters.AddWithValue("@VisitDate", reviewDto.VisitDate);
                cmd.Parameters.AddWithValue("@Comments", reviewDto.Comments);
                cmd.Parameters.AddWithValue("@FoodQualityRating", reviewDto.FoodQualityRating);
                cmd.Parameters.AddWithValue("@ServiceRating", reviewDto.ServiceRating);
                cmd.Parameters.AddWithValue("@AtmosphereRating", reviewDto.AtmosphereRating);
                cmd.Parameters.AddWithValue("@PriceRating", reviewDto.PriceRating);

                // *** TODO: Execute SP using _dbConnect ***
                // Modify SP to return SCOPE_IDENTITY() or use ExecuteScalar if it does
                // int newReviewId = Convert.ToInt32(_dbConnect.ExecuteScalarUsingCmdObj(cmd)); // Example if SP returns ID
                int result = 1; // Placeholder for DoUpdate result
                int newReviewId = new Random().Next(); // Placeholder ID
                await Task.Delay(10); // Simulate async work

                if (result > 0) // Check if insert was successful
                {
                    _logger.LogInformation("API: Review added successfully with ID {ReviewId}", newReviewId);
                    // TODO: Fetch the newly created Review object to return it
                    // Review createdReview = GetReviewByIdFromDb(newReviewId); // Helper needed
                    Review createdReview = new Review { ReviewID = newReviewId /* Populate other fields */ }; // Placeholder

                    // Return 201 Created with location header and the created object
                    return CreatedAtAction(nameof(GetReviewById), new { id = newReviewId }, createdReview);
                }
                else
                {
                    _logger.LogError("API: Failed to add review record to DB for Restaurant {RestaurantId} by User {UserId}", reviewDto.RestaurantID, userId);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error saving review information");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error adding review for Restaurant {RestaurantId} by User {UserId}", reviewDto.RestaurantID, userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error saving review");
            }
        }

        // PUT: api/reviews/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "reviewer")] // Only reviewers can edit
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto reviewDto) // Assuming UpdateReviewDto exists
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not identified.");
            }

            _logger.LogInformation("API: Attempting to update review {ReviewId} by User {UserId}", id, userId);

            // TODO: Add validation: Check if id from route matches id in DTO if applicable

            if (!ModelState.IsValid) return BadRequest(ModelState);

            // *** TODO: Add Ownership Check ***
            // Before updating, verify the review 'id' actually belongs to 'userId'
            // bool userOwnsReview = CheckReviewOwnership(id, userId); // Helper needed (calls DB)
            bool userOwnsReview = true; // Placeholder
            if (!userOwnsReview)
            {
                _logger.LogWarning("API: User {UserId} attempted to update review {ReviewId} they do not own.", userId, id);
                return Forbid(); // Or NotFound() depending on desired behavior
            }
            // *** End Ownership Check ***

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spUpdateReview";

                // TODO: Add parameters from reviewDto
                cmd.Parameters.AddWithValue("@ReviewID", id);
                cmd.Parameters.AddWithValue("@VisitDate", reviewDto.VisitDate);
                cmd.Parameters.AddWithValue("@Comments", reviewDto.Comments);
                cmd.Parameters.AddWithValue("@FoodQualityRating", reviewDto.FoodQualityRating);
                cmd.Parameters.AddWithValue("@ServiceRating", reviewDto.ServiceRating);
                cmd.Parameters.AddWithValue("@AtmosphereRating", reviewDto.AtmosphereRating);
                cmd.Parameters.AddWithValue("@PriceRating", reviewDto.PriceRating);
                // SP should only update if ReviewID matches AND potentially UserID matches

                // *** TODO: Execute SP using _dbConnect ***
                // int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                int result = 1; // Placeholder
                await Task.Delay(10); // Simulate async work

                if (result > 0)
                {
                    _logger.LogInformation("API: Review {ReviewId} updated successfully by User {UserId}", id, userId);
                    return NoContent(); // Standard success response for PUT
                }
                else
                {
                    // Could be not found, or no changes made, or permission issue if SP checks UserID
                    _logger.LogWarning("API: Review update failed for ID {ReviewId} (not found, no changes, or permission denied).", id);
                    return NotFound("Review not found or no changes made.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error updating review ID {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating review");
            }
        }

        // DELETE: api/reviews/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "reviewer")] // Only reviewers can delete
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not identified.");
            }

            _logger.LogInformation("API: Attempting to delete review {ReviewId} by User {UserId}", id, userId);

            // *** TODO: Add Ownership Check ***
            // Before deleting, verify the review 'id' actually belongs to 'userId'
            // bool userOwnsReview = CheckReviewOwnership(id, userId); // Helper needed (calls DB)
            bool userOwnsReview = true; // Placeholder
            if (!userOwnsReview)
            {
                _logger.LogWarning("API: User {UserId} attempted to delete review {ReviewId} they do not own.", userId, id);
                return Forbid(); // Or NotFound()
            }
            // *** End Ownership Check ***

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spDeleteReview";
                cmd.Parameters.AddWithValue("@ReviewID", id);
                // SP should ideally also check UserID for safety, or rely on ownership check above

                // *** TODO: Execute SP using _dbConnect ***
                // int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                int result = 1; // Placeholder
                await Task.Delay(10); // Simulate async work

                if (result > 0)
                {
                    _logger.LogInformation("API: Review {ReviewId} deleted successfully by User {UserId}", id, userId);
                    return NoContent(); // Standard success response for DELETE
                }
                else
                {
                    _logger.LogWarning("API: Review delete failed for ID {ReviewId} (not found or permission denied).", id);
                    return NotFound("Review not found or permission denied.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error deleting review ID {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting review");
            }
        }

        // GET: api/reviews/{id} - Added endpoint to support CreatedAtAction in POST
        [HttpGet("{id:int}", Name = "GetReviewById")] // Named route
        public async Task<ActionResult<Review>> GetReviewById(int id)
        {
            _logger.LogInformation("API: Getting review by ID {ReviewId}", id);
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spGetReviewById"; // Assumes this SP exists
                cmd.Parameters.AddWithValue("@ReviewID", id);

                // *** TODO: Execute SP using _dbConnect ***
                // DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                // *** TODO: Process DataSet and map result to Review object ***
                // Review review = MapDataRowToReview(ds?.Tables[0]?.Rows[0]); // Example mapping function needed

                Review review = null; // Placeholder
                await Task.Delay(10); // Simulate async work

                if (review == null)
                {
                    _logger.LogWarning("API: Review not found for ID {ReviewId}", id);
                    return NotFound();
                }

                return Ok(review); // Return domain model or a ReviewDto
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error getting review by ID {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error getting review");
            }
        }


        // GET: api/reviews/restaurant/{restaurantId}
        [HttpGet("restaurant/{restaurantId:int}")]
        public async Task<ActionResult<IEnumerable<ReviewViewModel>>> GetReviewsForRestaurant(int restaurantId, [FromQuery] int count = 0) // Optional count limit
        {
            _logger.LogInformation("API: Getting reviews for Restaurant ID {RestaurantId}", restaurantId);
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                // TODO: Need a SP like TP_spGetReviewsByRestaurant or adapt TP_spGetRecentReviews
                cmd.CommandText = "dbo.TP_spGetRecentReviews"; // Using existing for now
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                if (count > 0) cmd.Parameters.AddWithValue("@MaxCount", count);

                // *** TODO: Execute SP using _dbConnect ***
                // DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                // *** TODO: Process DataSet and map results to List<ReviewViewModel> ***
                // List<ReviewViewModel> results = MapDataSetToReviewViewModelList(ds); // Example mapping function needed

                List<ReviewViewModel> results = new List<ReviewViewModel>(); // Placeholder
                await Task.Delay(10); // Simulate async work

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error getting reviews for Restaurant ID {RestaurantId}", restaurantId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error getting reviews");
            }
        }

        // GET: api/reviews/user/{userId}
        [HttpGet("user/{userId:int}")]
        [Authorize] // Only authenticated users (or maybe only the specific user/admin?)
        public async Task<ActionResult<IEnumerable<ReviewViewModel>>> GetReviewsByUser(int userId)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(authenticatedUserIdString) || !int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            {
                return Unauthorized("User not identified.");
            }

            // *** TODO: Add Authorization Check ***
            // Ensure the authenticated user is requesting their own reviews (or is an admin)
            if (userId != authenticatedUserId /* && !User.IsInRole("Admin") */)
            {
                _logger.LogWarning("API: User {AuthUserId} attempted to access reviews for User {TargetUserId}.", authenticatedUserId, userId);
                return Forbid();
            }
            // *** End Authorization Check ***


            _logger.LogInformation("API: Getting reviews for User ID {UserId}", userId);
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.TP_spGetReviewsByUser";
                cmd.Parameters.AddWithValue("@UserID", userId);

                // *** TODO: Execute SP using _dbConnect ***
                // DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                // *** TODO: Process DataSet and map results to List<ReviewViewModel> ***
                // List<ReviewViewModel> results = MapDataSetToReviewViewModelList(ds);

                List<ReviewViewModel> results = new List<ReviewViewModel>(); // Placeholder
                await Task.Delay(10); // Simulate async work

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Error getting reviews for User ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error getting reviews");
            }
        }


        // TODO: Add helper methods for mapping DataSets/DataRows to Models/ViewModels/DTOs
        // TODO: Add helper method for checking review ownership

        // Placeholder DTOs (Define these in Models/DTOs)
        public class CreateReviewDto
        {
            public int RestaurantID { get; set; }
            public DateTime VisitDate { get; set; }
            public string Comments { get; set; }
            public int FoodQualityRating { get; set; }
            public int ServiceRating { get; set; }
            public int AtmosphereRating { get; set; }
            public int PriceRating { get; set; }
        }
        public class UpdateReviewDto
        {
            // May not need ID here if passed in route
            public DateTime VisitDate { get; set; }
            public string Comments { get; set; }
            public int FoodQualityRating { get; set; }
            public int ServiceRating { get; set; }
            public int AtmosphereRating { get; set; }
            public int PriceRating { get; set; }
        }

    }
}
