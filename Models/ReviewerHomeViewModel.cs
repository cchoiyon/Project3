using System;
using System.Collections.Generic; // For List

namespace Project3.Models
{
    /// <summary>
    /// ViewModel for the Reviewer Home page, holding all necessary data.
    /// Uses explicit properties style.
    /// </summary>
    [Serializable]
    public class ReviewerHomeViewModel
    {
        // Private backing fields
        private List<RestaurantViewModel> _featuredRestaurants;
        private List<RestaurantViewModel> _searchResults;
        private SearchCriteriaViewModel _searchCriteria; // To repopulate search form
        private List<string> _availableCuisines; // For populating checkboxes

        // Public properties
        public List<RestaurantViewModel> FeaturedRestaurants { get { return _featuredRestaurants; } set { _featuredRestaurants = value; } }
        public List<RestaurantViewModel> SearchResults { get { return _searchResults; } set { _searchResults = value; } }
        public SearchCriteriaViewModel SearchCriteria { get { return _searchCriteria; } set { _searchCriteria = value; } }
        public List<string> AvailableCuisines { get { return _availableCuisines; } set { _availableCuisines = value; } }


        // Constructor
        public ReviewerHomeViewModel()
        {
            // Initialize lists to avoid null reference errors in the view
            _featuredRestaurants = new List<RestaurantViewModel>();
            _searchResults = new List<RestaurantViewModel>();
            _searchCriteria = new SearchCriteriaViewModel();
            _availableCuisines = new List<string>(); // Populate this in the controller
        }
    }
}
