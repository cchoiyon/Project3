using System;
using System.Collections.Generic; // Required for List<>
using System.ComponentModel.DataAnnotations; // Required for validation attributes
using Project3.Models.Domain;
using Project3.Models.InputModels;

// Ensure this namespace matches your project structure
namespace Project3.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Restaurant Details page (Restaurant/Details view).
    /// Combines profile, reviews, photos, and calculated display data.
    /// NOTE: Uses explicit properties with private backing fields.
    /// </summary>
    [Serializable]
    public class RestaurantDetailViewModel
    {
        // Private backing fields
        private int _restaurantID;
        private Restaurant _profile; // The full restaurant profile
        private List<ReviewViewModel> _reviews; // List of reviews for display
        private List<Photo> _photos; // List of photos for the gallery (using Photo domain model)
        private string _averageRatingDisplay; // e.g., "4.5 / 5 stars"
        private string _averagePriceLevelDisplay; // e.g., "$$$"

        // Public Properties
        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }   

        public Restaurant Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }

        public List<ReviewViewModel> Reviews
        {
            get { return _reviews; }
            set { _reviews = value; }
        }

        // Using Photo domain model here now
        public List<Photo> Photos
        {
            get { return _photos; }
            set { _photos = value; }
        }

        public string AverageRatingDisplay
        {
            get { return _averageRatingDisplay; }
            set { _averageRatingDisplay = value; }
        }

        public string AveragePriceLevelDisplay
        {
            get { return _averagePriceLevelDisplay; }
            set { _averagePriceLevelDisplay = value; }
        }

        public int AverageRating { get; set; }

        // Parameterless constructor
        public RestaurantDetailViewModel()
        {
            // Initialize lists to prevent null reference errors in the view
            _profile = new Restaurant();
            _reviews = new List<ReviewViewModel>();
            _photos = new List<Photo>(); // Updated initialization
        }
    }
}
