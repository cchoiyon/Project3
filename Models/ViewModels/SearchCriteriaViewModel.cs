using System;
using System.Collections.Generic; // For List
using System.ComponentModel.DataAnnotations; // For Display attribute

namespace Project3.Models.ViewModels
{
    /// <summary>
    /// ViewModel for holding search criteria from the form.
    /// All fields are optional to allow flexible searching.
    /// </summary>
    [Serializable]
    public class SearchCriteriaViewModel
    {
        // Private backing fields
        // Note: Binding directly to List<string> from checkboxes can be tricky without Tag Helpers.
        // It might be simpler to get individual checkbox values or a comma-separated string.
        // Let's assume a simpler string approach for now, controller can parse it.
        private string _cuisineInput; // e.g., "Italian,Mexican" or handle individually
        private string _city;
        private string _state;
        private List<string> _availableCuisines;

        // Public properties - all optional with no validation
        [Display(Name = "Cuisine Type")]
        public string CuisineInput 
        { 
            get { return _cuisineInput; } 
            set { _cuisineInput = value?.Trim(); } 
        }

        [Display(Name = "City")]
        public string City 
        { 
            get { return _city; } 
            set { _city = value?.Trim(); } 
        }

        [Display(Name = "State")]
        public string State 
        { 
            get { return _state; } 
            set { _state = value?.Trim()?.ToUpper(); } 
        }

        public List<string> AvailableCuisines 
        { 
            get { return _availableCuisines ?? (_availableCuisines = new List<string>()); }
            set { _availableCuisines = value; } 
        }

        // Constructor
        public SearchCriteriaViewModel() 
        {
            _availableCuisines = new List<string>();
        }
    }
}
