using System;

namespace Project3.Models
{
    /// <summary>
    /// ViewModel representing restaurant data to be displayed, including calculated fields.
    /// Uses explicit properties style.
    /// </summary>
    [Serializable]
    public class RestaurantViewModel
    {
        // Fields from Restaurant model
        private int _restaurantID;
        private string _name;
        private string _address;
        private string _city;
        private string _state;
        private string _zipCode;
        private string _cuisine;
        private string _logoPhoto; // For display in lists

        // Calculated fields from Reviews
        private double _overallRating;
        private int _reviewCount;
        private double _averagePriceRating; // For price level display

        // Properties
        public int RestaurantID { get { return _restaurantID; } set { _restaurantID = value; } }
        public string Name { get { return _name; } set { _name = value; } }
        public string Address { get { return _address; } set { _address = value; } }
        public string City { get { return _city; } set { _city = value; } }
        public string State { get { return _state; } set { _state = value; } }
        public string ZipCode { get { return _zipCode; } set { _zipCode = value; } }
        public string Cuisine { get { return _cuisine; } set { _cuisine = value; } }
        public string LogoPhoto { get { return _logoPhoto; } set { _logoPhoto = value; } }
        public double OverallRating { get { return _overallRating; } set { _overallRating = value; } }
        public int ReviewCount { get { return _reviewCount; } set { _reviewCount = value; } }
        public double AveragePriceRating { get { return _averagePriceRating; } set { _averagePriceRating = value; } }

        // Constructor
        public RestaurantViewModel() { }

        // You might add a constructor that takes a DataRow to populate this easily
    }
}

