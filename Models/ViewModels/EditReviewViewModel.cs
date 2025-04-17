using System.ComponentModel.DataAnnotations; // Required for validation attributes

namespace Project3.Models.ViewModels
{
    // This ViewModel holds the data needed for the Edit Review form/action
    public class EditReviewViewModel
    {
        // Hidden field in the form to keep track of which review is being edited
        [Required]
        public int ReviewId { get; set; }

        // Display the restaurant name, but don't allow editing it here
        [Display(Name = "Restaurant")]
        public string RestaurantName { get; set; } // Populate this in the GET action

        // Editable field for the rating
        [Required(ErrorMessage = "Please enter a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] // Example range, adjust if needed
        // Use DisplayFormat if you want to control how it appears in the form, e.g., for decimals
        // [DisplayFormat(DataFormatString = "{0:0.0}", ApplyFormatInEditMode = true)]
        public decimal Rating { get; set; } // Match data type used in ReviewDto/database (decimal, int?)

        // Editable field for the comment
        [DataType(DataType.MultilineText)] // Suggests a larger text area in the view
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")] // Example length limit
        public string Comment { get; set; }

        // You might include other fields if necessary, like the original ReviewDate for display
        // [Display(Name = "Review Date")]
        // [DataType(DataType.Date)]
        // public DateTime ReviewDate { get; set; }
    }
}
