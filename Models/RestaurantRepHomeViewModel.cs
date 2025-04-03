using System;
using System.Collections.Generic; // For List

namespace Project3.Models
{
    /// <summary>
    /// ViewModel for the Restaurant Rep Home page dashboard.
    /// Uses explicit properties style.
    /// </summary>
    [Serializable]
    public class RestaurantRepHomeViewModel
    {
        // Private backing fields
        private Restaurant _restaurantProfile; // The rep's restaurant profile
        private List<Review> _recentReviews; // List of recent reviews for their restaurant
        private List<Reservation> _pendingReservations; // List of pending reservations

        // Public properties
        public Restaurant RestaurantProfile
        {
            get { return _restaurantProfile; }
            set { _restaurantProfile = value; }
        }
        public List<Review> RecentReviews
        {
            get { return _recentReviews; }
            set { _recentReviews = value; }
        }
        public List<Reservation> PendingReservations
        {
            get { return _pendingReservations; }
            set { _pendingReservations = value; }
        }

        // Constructor
        public RestaurantRepHomeViewModel()
        {
            // Initialize to avoid null issues in the view
            _restaurantProfile = new Restaurant();
            _recentReviews = new List<Review>();
            _pendingReservations = new List<Reservation>();
        }
    }
}
