using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Models.DTOs
{
    /// <summary>
    /// Data returned FROM the API representing a review.
    /// Could be the same as ReviewViewModel or tailored.
    /// </summary>
    public class ReviewDto
    {
        public int ReviewID { get; set; }
        public int RestaurantID { get; set; }
        public string RestaurantName { get; set; } // Include name if API performs JOIN
        public int UserID { get; set; } // Might omit this depending on use case
        public string ReviewerUsername { get; set; } // Include if API performs JOIN
        public DateTime VisitDate { get; set; }
        public string Comments { get; set; }
        public int FoodQualityRating { get; set; }
        public int ServiceRating { get; set; }
        public int AtmosphereRating { get; set; }
        public int PriceRating { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
