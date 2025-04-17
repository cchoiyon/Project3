using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when updating a photo's caption (e.g., via PUT api/photos/{photoId}).
    /// </summary>
    public class UpdatePhotoCaptionDto
    {
        [Required]
        [StringLength(500)]
        public string NewCaption { get; set; }
    }
}
