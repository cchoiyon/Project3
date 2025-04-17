using System;
using System.ComponentModel.DataAnnotations; // For potential future validation attributes
using Microsoft.AspNetCore.Http;

// Ensure this namespace matches your folder structure
namespace Project3.Models.ViewModels
{
    /// <summary>
    /// ViewModel representing restaurant data for display (e.g., search results, cards)
    /// AND for editing in the profile manager form. Includes calculated fields.
    /// Uses explicit properties style.
    /// </summary>
    [Serializable]
    public class RestaurantViewModel
    {
        // --- Fields from Restaurant domain model ---
        private int _restaurantID;
        private string _name;
        private string _address;
        private string _city;
        private string _state;
        private string _zipCode;
        private string _cuisine;
        private string _hours; // Added
        private string _contact; // Added (e.g., Phone)
        private string _profilePhoto; // Added
        private string _logoPhoto;
        private string _marketingDescription; // Added
        private string _websiteURL; // Added
        private string _socialMedia; // Added
        private string _owner; // Added
        // CreatedDate is usually not needed in ViewModel

        // --- Calculated/Aggregated fields (populated by API or Controller) ---
        private double _overallRating;
        private int _reviewCount;
        private double _averagePriceRating;

        // --- Properties ---

        // Identifiers
        public int RestaurantID { get { return _restaurantID; } set { _restaurantID = value; } }

        // Core Info (Editable in ManageProfile)
        [Required(ErrorMessage = "Restaurant name is required")]
        [StringLength(100, ErrorMessage = "Restaurant name cannot exceed 100 characters")]
        public string Name { get { return _name; } set { _name = value; } }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get { return _address; } set { _address = value; } }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string City { get { return _city; } set { _city = value; } }

        [StringLength(2, ErrorMessage = "State must be 2 characters")]
        public string State { get { return _state; } set { _state = value; } }

        [StringLength(10, ErrorMessage = "ZIP code cannot exceed 10 characters")]
        public string ZipCode { get { return _zipCode; } set { _zipCode = value; } }

        [StringLength(50, ErrorMessage = "Cuisine cannot exceed 50 characters")]
        public string Cuisine { get { return _cuisine; } set { _cuisine = value; } }

        [StringLength(500, ErrorMessage = "Hours cannot exceed 500 characters")]
        public string Hours { get { return _hours; } set { _hours = value; } }

        [StringLength(100, ErrorMessage = "Contact information cannot exceed 100 characters")]
        public string Contact { get { return _contact; } set { _contact = value; } }

        [StringLength(1000, ErrorMessage = "Marketing description cannot exceed 1000 characters")]
        public string MarketingDescription { get { return _marketingDescription; } set { _marketingDescription = value; } }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters")]
        public string WebsiteURL { get { return _websiteURL; } set { _websiteURL = value; } }

        [StringLength(500, ErrorMessage = "Social media links cannot exceed 500 characters")]
        public string SocialMedia { get { return _socialMedia; } set { _socialMedia = value; } }

        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters")]
        public string Owner { get { return _owner; } set { _owner = value; } }

        // Photo URLs (Primary ones stored in TP_Restaurants)
        public string? ProfilePhoto { get; set; }
        public string? LogoPhoto { get; set; }

        // Calculated/Aggregated Properties (Read-only in forms, set by Controller/API)
        [Display(Name = "Overall Rating")]
        public double OverallRating { get { return _overallRating; } set { _overallRating = value; } } // Typically set from API result

        [Display(Name = "Number of Reviews")]
        public int ReviewCount { get { return _reviewCount; } set { _reviewCount = value; } } // Typically set from API result

        [Display(Name = "Average Price")]
        public double AveragePriceRating { get { return _averagePriceRating; } set { _averagePriceRating = value; } } // Typically set from API result

        public double AverageRating { get; set; }
        public int AveragePriceLevel { get; set; }

        // File upload properties - already optional
        public IFormFile? ProfilePhotoFile { get; set; }
        public IFormFile? LogoPhotoFile { get; set; }

        // Constructor
        public RestaurantViewModel() { }

        // You might add a constructor that takes a Restaurant domain object
        // public RestaurantViewModel(Restaurant restaurant) { /* map properties */ }

        // You could also add mapping methods here or use a library like AutoMapper
    }
}
