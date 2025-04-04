using System; // Needed for DateTime

namespace Project3.Models.DTOs
{
    // This DTO (Data Transfer Object) is used to carry review data,
    // often between the database/API and the view models/views.
    public class ReviewDto
    {
        // Unique identifier for the review
        public int ReviewId { get; set; }

        // Name of the restaurant being reviewed
        public string RestaurantName { get; set; }

        // The rating given in the review
        public decimal Rating { get; set; } // Ensure data type matches your needs

        // The text comment of the review
        public string Comment { get; set; }

        // The date the review was created or submitted
        public DateTime ReviewDate { get; set; }

        // *** VERIFY THIS PROPERTY EXISTS EXACTLY AS SHOWN ***
        // This holds the username of the person who wrote the review.
        public string ReviewerUsername { get; set; }

        // You might also include other relevant fields like:
        // public int RestaurantId { get; set; }

        // Constructor (optional)
        public ReviewDto()
        {
            // Initialize default values if necessary
            // RestaurantName = string.Empty;
            // Comment = string.Empty;
            // ReviewerUsername = "Anonymous"; // Or string.Empty
        }
    }
}
