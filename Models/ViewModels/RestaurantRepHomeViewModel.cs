using System;
using System.Collections.Generic; // For List
using Project3.Models.Domain;
using Project3.Models.InputModels; // *** ADDED: Using statement for ReviewViewModel ***

namespace Project3.Models.ViewModels
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
                                               // *** FIXED: Changed type from List<Review> to List<ReviewViewModel> ***
        private List<ReviewViewModel> _recentReviews;
        private List<Reservation> _pendingReservations; // List of pending reservations

        // Public properties
        public Restaurant RestaurantProfile
        {
            get { return _restaurantProfile; }
            set { _restaurantProfile = value; }
        }

        // *** FIXED: Changed type from List<Review> to List<ReviewViewModel> ***
        public List<ReviewViewModel> RecentReviews
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
            // *** FIXED: Initialize with correct type ***
            _recentReviews = new List<ReviewViewModel>();
            _pendingReservations = new List<Reservation>();
        }
    }
}
