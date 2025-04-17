using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when updating an existing review.
    /// </summary>
    public class UpdateReviewDto
    {
        // ID is typically passed in the route (api/reviews/{id})
        [Required]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Required]
        [Range(1, 5)]
        public int FoodQualityRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int ServiceRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int AtmosphereRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int PriceRating { get; set; }
    }
}
