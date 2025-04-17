using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when adding a photo (e.g., via POST api/restaurants/{id}/photos).
    /// URL is provided after file is saved by the caller (MVC controller or API itself).
    /// </summary>
    public class AddPhotoDto
    {
        [Required]
        public string PhotoURL { get; set; } // Relative URL after saving file

        [StringLength(500)]
        public string? Caption { get; set; }
    }
}
