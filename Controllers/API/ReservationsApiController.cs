using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project3.Models.Domain; // Assuming Reservation model is here
using Project3.Models.DTOs;    // Ensure you are using the DTOs from the correct namespace
using Project3.Utilities;    // For Connection
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq; // Needed for mapping potentially
using System.Security.Claims; // For UserID / Role checks
using System.Threading.Tasks;

namespace Project3.Controllers.API
{
    [Route("api/[controller]")] // Route will be /api/ReservationsApi
    [ApiController]
    public class ReservationsApiController : ControllerBase
    {
        private readonly ILogger<ReservationsApiController> _logger;
        private readonly Connection _dbConnect; // Injected via DI

        // Constructor
        public ReservationsApiController(ILogger<ReservationsApiController> logger, Connection dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        // POST: api/ReservationsApi
        [HttpPost]
        [AllowAnonymous] // Or [Authorize] if only logged-in users can reserve?
        public async Task<ActionResult<Reservation>> CreateReservation([FromBody] CreateReservationDto reservationDto)
        {
            _logger.LogInformation("API: Attempting to create reservation for Restaurant {RestaurantId}", reservationDto.RestaurantID);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int parsedUserId)) { userId = parsedUserId; }
                else { _logger.LogWarning("API: Could not parse UserID from authenticated user claims."); }
            }

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spAddReservation");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RestaurantID", reservationDto.RestaurantID);
                cmd.Parameters.AddWithValue("@UserID", userId.HasValue ? (object)userId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ReservationDateTime", reservationDto.ReservationDateTime);
                cmd.Parameters.AddWithValue("@PartySize", reservationDto.PartySize);
                cmd.Parameters.AddWithValue("@ContactName", reservationDto.ContactName);
                cmd.Parameters.AddWithValue("@Phone", reservationDto.Phone);
                cmd.Parameters.AddWithValue("@Email", reservationDto.Email);
                cmd.Parameters.AddWithValue("@SpecialRequests", string.IsNullOrEmpty(reservationDto.SpecialRequests) ? DBNull.Value : reservationDto.SpecialRequests);
                cmd.Parameters.AddWithValue("@Status", "Pending");

                object result = _dbConnect.ExecuteScalarFunction(cmd);
                int newReservationId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                if (newReservationId > 0)
                {
                    _logger.LogInformation("API: Reservation {ReservationId} created for Restaurant {RestaurantId}", newReservationId, reservationDto.RestaurantID);
                    Reservation createdReservation = GetReservationByIdInternal(newReservationId);
                    if (createdReservation != null)
                    {
                        return CreatedAtAction(nameof(GetReservationById), new { id = newReservationId }, createdReservation);
                    }
                }
                else
                {
                    _logger.LogError("API: Failed to add reservation record to DB (ExecuteScalar returned 0 or null) for Restaurant {RestaurantId}", reservationDto.RestaurantID);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error saving reservation data.");
                }

                // Add return statement for the case when createdReservation is null
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving created reservation.");

            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error creating reservation for Restaurant {RestaurantId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    reservationDto.RestaurantID, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error creating reservation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error creating reservation for Restaurant {RestaurantId}", reservationDto.RestaurantID);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating reservation.");
            }
        }

        // GET: api/ReservationsApi/{id}
        [HttpGet("{id:int}", Name = nameof(GetReservationById))]
        [Authorize] // TODO: Add more specific authorization
        public async Task<ActionResult<Reservation>> GetReservationById(int id)
        {
            _logger.LogInformation("API: Getting reservation by ID {ReservationId}", id);
            // TODO: Add Authorization Check
            try
            {
                Reservation reservation = GetReservationByIdInternal(id);
                if (reservation == null) { return NotFound(); }
                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting reservation by ID {ReservationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving reservation data.");
            }
        }


        // GET: api/ReservationsApi/restaurant/{restaurantId}
        [HttpGet("restaurant/{restaurantId:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservationsForRestaurant(int restaurantId, [FromQuery] string? status)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            // TODO: Verify rep owns this restaurantId
            // bool isRepForRestaurant = await CheckIfUserIsRepForRestaurant(authenticatedUserId, restaurantId);
            // if (!isRepForRestaurant) return Forbid();

            _logger.LogInformation("API: Getting reservations for Restaurant ID {RestaurantId} with Status={Status} by Rep {RepUserId}", restaurantId, status ?? "All", authenticatedUserId);
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReservationsByRestaurant");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                cmd.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                List<Reservation> reservations = MapDataSetToReservationList(ds); // Implement helper

                return Ok(reservations);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error getting reservations for Restaurant ID {RestaurantId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    restaurantId, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error retrieving reservations.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error getting reservations for Restaurant ID {RestaurantId}", restaurantId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving reservations.");
            }
        }

        // PUT: api/ReservationsApi/{id}/status
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            _logger.LogInformation("API: Attempting to update status for reservation {ReservationId} to {NewStatus} by Rep {UserId}", id, statusDto.Status, authenticatedUserId);

            if (!ModelState.IsValid) return BadRequest(ModelState);

            // TODO: Add Ownership Check
            // bool repOwnsReservation = await CheckReservationOwnership(id, authenticatedUserId);
            // if (!repOwnsReservation) return Forbid();

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spUpdateReservationStatus");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);
                cmd.Parameters.AddWithValue("@NewStatus", statusDto.Status);
                // Optionally pass @RestaurantID_Check = authenticatedUserId

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Reservation {ReservationId} status updated to {NewStatus} by Rep {UserId}", id, statusDto.Status, authenticatedUserId);
                    // TODO: Optionally send email notification
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Reservation status update failed for ID {ReservationId} (Update returned 0 rows affected - check ownership/existence).", id);
                    return NotFound("Reservation not found or update failed.");
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error updating status for reservation ID {ReservationId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    id, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error updating reservation status.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error updating status for reservation ID {ReservationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating reservation status.");
            }
        }

        // DELETE: api/ReservationsApi/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            _logger.LogInformation("API: Attempting to delete reservation {ReservationId} by Rep {UserId}", id, authenticatedUserId);

            // TODO: Add Ownership Check
            // bool repOwnsReservation = await CheckReservationOwnership(id, authenticatedUserId);
            // if (!repOwnsReservation) return Forbid();

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spDeleteReservation");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);
                // Optionally pass @RestaurantID_Check = authenticatedUserId

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Reservation {ReservationId} deleted successfully by Rep {UserId}", id, authenticatedUserId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Reservation delete failed for ID {ReservationId} (Update returned 0 rows affected - check ownership/existence).", id);
                    return NotFound("Reservation not found or delete failed.");
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error deleting reservation ID {ReservationId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    id, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error deleting reservation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error deleting reservation ID {ReservationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting reservation.");
            }
        }


        // ================== Helper Methods ==================

        private Reservation GetReservationByIdInternal(int id)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReservationById");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return MapDataRowToReservation(ds.Tables[0].Rows[0]);
                }
                else
                {
                    _logger.LogWarning("API Internal: Reservation not found for ID {ReservationId}", id);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Internal: Error retrieving reservation by ID {ReservationId}", id);
                throw;
            }
        }

        private Reservation MapDataRowToReservation(DataRow dr)
        {
            if (dr == null) return null;
            try
            {
                // TODO: Verify these column names match your SP output exactly
                return new Reservation
                {
                    ReservationID = Convert.ToInt32(dr["ReservationID"]),
                    RestaurantID = Convert.ToInt32(dr["RestaurantID"]),
                    UserID = dr["UserID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["UserID"]),
                    ReservationDateTime = Convert.ToDateTime(dr["ReservationDateTime"]),
                    PartySize = Convert.ToInt32(dr["PartySize"]),
                    ContactName = dr["ContactName"]?.ToString(),
                    Phone = dr["Phone"]?.ToString(),
                    Email = dr["Email"]?.ToString(),
                    SpecialRequests = dr["SpecialRequests"]?.ToString(),
                    Status = dr["Status"]?.ToString(),
                    // *** FIXED Property Name ***
                    CreatedDate = Convert.ToDateTime(dr["DateCreated"]) // Assuming DB column is DateCreated
                    // *** END FIX ***
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping DataRow to Reservation object. Check column names and types in MapDataRowToReservation.");
                return null;
            }
        }

        private List<Reservation> MapDataSetToReservationList(DataSet ds)
        {
            var reservations = new List<Reservation>();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Reservation res = MapDataRowToReservation(dr);
                    if (res != null) { reservations.Add(res); }
                }
            }
            return reservations;
        }

        // TODO: Implement helper methods for authorization/ownership checks if needed

    }
}
