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
        // Ensure this property exists if used in the view (like in ManageReviews.cshtml)
        public string RestaurantName { get; set; }

        // FIX: Added the missing 'Rating' property.
        // Adjust the data type (decimal, int, double?) if needed based on how you store ratings.
        public decimal Rating { get; set; }

        // The text comment of the review
        // Ensure this property exists if used in the view
        public string Comment { get; set; }

        // The date the review was created or submitted
        // Ensure this property exists if used in the view
        public DateTime ReviewDate { get; set; }

        // You might also include other relevant fields like:
        // public string ReviewerUsername { get; set; }
        // public int RestaurantId { get; set; }

        // Constructor (optional)
        public ReviewDto()
        {
            // Initialize default values if necessary
            // RestaurantName = string.Empty;
            // Comment = string.Empty;
        }
    }
}
