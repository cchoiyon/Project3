using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Required for validation attributes

// Ensure this namespace matches your project structure
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object (DTO) sent TO the API when updating a restaurant's profile.
    /// Contains fields from the Restaurant domain model that are intended to be updatable
    /// by a restaurant representative.
    /// Updated to align with the Restaurant domain model properties.
    /// </summary>
    public class UpdateRestaurantProfileDto
    {
        /// <summary>
        /// The unique identifier for the restaurant being updated.
        /// Required to identify the correct record in the database.
        /// </summary>
        [Required(ErrorMessage = "Restaurant ID is required to update a profile.")]
        public int RestaurantID { get; set; }

        /// <summary>
        /// The name of the restaurant.
        /// </summary>
        [Required(ErrorMessage = "Restaurant Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; }

        /// <summary>
        /// The street address of the restaurant. Optional.
        /// </summary>
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; } // Nullable string

        /// <summary>
        /// The city where the restaurant is located. Optional.
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; } // Added, Nullable string

        /// <summary>
        /// The state where the restaurant is located. Optional.
        /// </summary>
        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
        public string? State { get; set; } // Added, Nullable string

        /// <summary>
        /// The postal (ZIP) code for the restaurant's location. Optional.
        /// </summary>
        [StringLength(10, ErrorMessage = "Zip Code cannot exceed 10 characters.")]
        public string? ZipCode { get; set; } // Added, Nullable string

        /// <summary>
        /// The type of cuisine the restaurant serves (e.g., Italian, Mexican). Optional.
        /// </summary>
        [StringLength(100, ErrorMessage = "Cuisine type cannot exceed 100 characters.")]
        public string? Cuisine { get; set; } // Added (replaces 'Type'), Nullable string

        /// <summary>
        /// The operating hours of the restaurant. Optional.
        /// </summary>
        [StringLength(255, ErrorMessage = "Hours description cannot exceed 255 characters.")]
        public string? Hours { get; set; } // Added, Nullable string

        /// <summary>
        /// Contact information, typically a phone number. Optional.
        /// </summary>
        [StringLength(100, ErrorMessage = "Contact information cannot exceed 100 characters.")]
        public string? Contact { get; set; } // Added (replaces 'Phone'), Nullable string

        /// <summary>
        /// A description used for marketing purposes. Optional.
        /// </summary>
        [DataType(DataType.MultilineText)] // Suggests a larger text input area in UI
        public string? MarketingDescription { get; set; } // Added (replaces 'Description'), Nullable string

        /// <summary>
        /// The URL of the restaurant's official website. Optional. Must be a valid URL format.
        /// </summary>
        [Url(ErrorMessage = "Please enter a valid Website URL (e.g., http://www.example.com).")]
        [StringLength(255, ErrorMessage = "Website URL cannot exceed 255 characters.")]
        public string? WebsiteURL { get; set; } // Nullable string

        /// <summary>
        /// Links to social media profiles (e.g., Facebook, Instagram). Optional.
        /// </summary>
        public string? SocialMedia { get; set; } // Added, Nullable string

        /// <summary>
        /// The name of the restaurant owner or primary contact person. Optional.
        /// </summary>
        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters.")]
        public string? Owner { get; set; } // Added, Nullable string

        /// <summary>
        /// URL or path to the restaurant's main profile photo. Optional.
        /// Handled by separate photo upload logic, but URL might be stored/updated here.
        /// </summary>
        public string? ProfilePhoto { get; set; } // Kept as nullable string

        /// <summary>
        /// URL or path to the restaurant's logo image. Optional.
        /// Handled by separate photo upload logic, but URL might be stored/updated here.
        /// </summary>
        public string? LogoPhoto { get; set; } // Kept as nullable string

        // Constructor (optional, can be useful)
        public UpdateRestaurantProfileDto()
        {
            // Initialize required fields if necessary, though handled by [Required]
            Name = string.Empty; // Initialize to avoid null warnings if not set immediately
        }
    }
}
