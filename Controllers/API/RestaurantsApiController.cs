using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project3.Models.Domain; // Using user's Restaurant model structure
using Project3.Models.DTOs;    // Using updated DTO
using Project3.Utilities;    // For DBConnect
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project3.Controllers.API
{
    [Route("api/[controller]")] // Route will be /api/RestaurantsApi
    [ApiController]
    public class RestaurantsApiController : ControllerBase
    {
        private readonly ILogger<RestaurantsApiController> _logger;
        private readonly DBConnect _dbConnect;

        public RestaurantsApiController(ILogger<RestaurantsApiController> logger, DBConnect dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        // GET: api/RestaurantsApi/{id}
        [HttpGet("{id:int}", Name = "GetRestaurantById")]
        [AllowAnonymous]
        public async Task<ActionResult<Restaurant>> GetRestaurantById(int id)
        {
            _logger.LogInformation("API: Attempting to get restaurant by ID: {RestaurantId}", id);
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetRestaurantByID"); // Verify SP name
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", id);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    // *** Updated mapping logic ***
                    Restaurant restaurant = MapDataRowToRestaurant(ds.Tables[0].Rows[0]); // Use updated helper
                    if (restaurant != null)
                    {
                        _logger.LogInformation("API: Found restaurant ID: {RestaurantId}", id);
                        return Ok(restaurant);
                    }
                    else { return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error processing restaurant data.")); }
                }
                else
                {
                    _logger.LogWarning("API: Restaurant not found for ID: {RestaurantId}", id);
                    return NotFound(new ErrorResponseDto($"Restaurant with ID {id} not found."));
                }
            }
            catch (SqlException sqlEx) { /* ... Log ... */ return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Database error retrieving restaurant data.")); }
            catch (Exception ex) { /* ... Log ... */ return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error retrieving restaurant data.")); }
        }

        // PUT: api/RestaurantsApi/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> UpdateRestaurantProfile(int id, [FromBody] UpdateRestaurantProfileDto profileDto) // Uses updated DTO
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized(new ErrorResponseDto("User identifier claim is invalid or missing.")); }

            _logger.LogInformation("API: Attempting to update profile for Restaurant {RestaurantId} by Rep {RepUserId}", id, authenticatedUserId);

            // Implement proper authorization check
            bool isAuthorized = User.IsInRole("restaurantRep") && authenticatedUserId == id;
            if (!isAuthorized)
            {
                _logger.LogWarning("API: Rep {RepUserId} forbidden to update profile for Restaurant {RestaurantId}.", authenticatedUserId, id);
                return Forbid();
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);
            // Check if ID in route matches ID in body (now possible with updated DTO)
            if (id != profileDto.RestaurantID) return BadRequest(new ErrorResponseDto("Restaurant ID mismatch between route and body."));


            try
            {
                // Ensure stored procedure exists and parameters match
                SqlCommand cmd = new SqlCommand("dbo.TP_spUpdateRestaurantProfile");
                cmd.CommandType = CommandType.StoredProcedure;

                // Add parameters based on Updated DTO and User's Restaurant Model
                cmd.Parameters.AddWithValue("@RestaurantID", id); // Use ID from route
                cmd.Parameters.AddWithValue("@Name", profileDto.Name);
                cmd.Parameters.AddWithValue("@Address", (object)profileDto.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@City", (object)profileDto.City ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@State", (object)profileDto.State ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ZipCode", (object)profileDto.ZipCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cuisine", (object)profileDto.Cuisine ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hours", (object)profileDto.Hours ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Contact", (object)profileDto.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MarketingDescription", (object)profileDto.MarketingDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WebsiteURL", (object)profileDto.WebsiteURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SocialMedia", (object)profileDto.SocialMedia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Owner", (object)profileDto.Owner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object)profileDto.ProfilePhoto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LogoPhoto", (object)profileDto.LogoPhoto ?? DBNull.Value);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Profile updated successfully for Restaurant {RestaurantId} by Rep {RepUserId}", id, authenticatedUserId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Profile update failed for Restaurant {RestaurantId} (DB update returned 0 rows affected).", id);
                    return NotFound(new ErrorResponseDto($"Restaurant with ID {id} not found or update failed."));
                }
            }
            catch (SqlException sqlEx) { /* ... Log ... */ return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Database error updating restaurant profile.")); }
            catch (Exception ex) { /* ... Log ... */ return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error updating restaurant profile.")); }
        }


        // ================== Helper Methods ==================

        /// <summary>
        /// Helper method to map a DataRow to a Restaurant object (User's Version).
        /// </summary>
        private Restaurant MapDataRowToRestaurant(DataRow dr)
        {
            if (dr == null) return null;
            try
            {
                // *** Updated to map to User's Restaurant properties ***
                var restaurant = new Restaurant(); // Use parameterless constructor

                restaurant.RestaurantID = Convert.ToInt32(dr["RestaurantID"]); // Assuming column name is RestaurantID
                restaurant.Name = dr["Name"]?.ToString() ?? string.Empty; // Handle null, provide default for non-nullable string
                restaurant.Address = dr["Address"]?.ToString() ?? string.Empty;
                restaurant.City = dr["City"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.State = dr["State"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.ZipCode = dr["ZipCode"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.Cuisine = dr["Cuisine"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.Hours = dr["Hours"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.Contact = dr["Contact"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.MarketingDescription = dr["MarketingDescription"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.WebsiteURL = dr["WebsiteURL"]?.ToString() ?? string.Empty; // Kept mapping
                restaurant.SocialMedia = dr["SocialMedia"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.Owner = dr["Owner"]?.ToString() ?? string.Empty; // Added mapping
                restaurant.ProfilePhoto = dr["ProfilePhoto"]?.ToString() ?? string.Empty; // Kept mapping
                restaurant.LogoPhoto = dr["LogoPhoto"]?.ToString() ?? string.Empty; // Kept mapping
                restaurant.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]); // Kept mapping (Verify column name)

                // Removed mapping for properties not in user's model: Type, Description, Phone, AverageRating, Status

                return restaurant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping DataRow to Restaurant object (User's Version). Check column names and types.");
                return null;
            }
        }
        // TODO: Update MapDataSetToRestaurantList if needed
        private List<Restaurant> MapDataSetToRestaurantList(DataSet ds)
        {
            var restaurants = new List<Restaurant>();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Restaurant res = MapDataRowToRestaurant(dr); // Reuse single row mapper
                    if (res != null)
                    {
                        restaurants.Add(res);
                    }
                }
            }
            return restaurants;
        }
    }
}
