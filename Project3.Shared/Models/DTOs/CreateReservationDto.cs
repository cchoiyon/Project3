using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when creating a new reservation.
    /// </summary>
    public class CreateReservationDto
    {
        [Required]
        public int RestaurantID { get; set; }

        // UserID is set by API if user is authenticated, null otherwise
        // No UserID property needed here as it's determined by the API context

        [Required(ErrorMessage = "Reservation Date and Time are required.")]
        [DataType(DataType.DateTime)]
        // API or Controller should validate that this date is in the future
        public DateTime ReservationDateTime { get; set; }

        [Required(ErrorMessage = "Party Size is required.")]
        [Range(1, 100, ErrorMessage = "Party Size must be between 1 and 100.")] // Example range
        public int PartySize { get; set; }

        [Required(ErrorMessage = "Contact Name is required.")]
        [StringLength(100)]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number format.")]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(100)]
        public string Email { get; set; }

        [DataType(DataType.MultilineText)]
        public string? SpecialRequests { get; set; } // Nullable string

        // Parameterless constructor (good practice)
        public CreateReservationDto() { }
    }
}
