using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when creating a new review.
    /// </summary>
    public class CreateReviewDto
    {
        [Required]
        public int RestaurantID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; } // Or just DateOnly?

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

        // UserID is determined by the API based on the authenticated user
    }
}
