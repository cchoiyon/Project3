using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when updating a reservation's status.
    /// </summary>
    public class UpdateStatusDto
    {
        [Required(ErrorMessage = "New status is required.")] // Added specific error message
        [StringLength(50)] // Match DB column
        public string Status { get; set; } // e.g., "Confirmed", "Cancelled"

        // Parameterless constructor (good practice, often needed for model binding/deserialization)
        public UpdateStatusDto() { }

        // Optional: Constructor for convenience
        public UpdateStatusDto(string status)
        {
            Status = status;
        }
    }
}
