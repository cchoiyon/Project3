using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data returned FROM the API representing a reservation.
    /// Could potentially just use the Reservation domain model if no shaping is needed.
    /// </summary>
    public class ReservationDto
    {
        public int ReservationID { get; set; }
        public int RestaurantID { get; set; }
        public string RestaurantName { get; set; } // Include if API performs JOIN
        public int? UserID { get; set; }
        public string? Username { get; set; } // Include if API performs JOIN
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime ReservationDateTime { get; set; }
        public int PartySize { get; set; }
        public string? SpecialRequests { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
