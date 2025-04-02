using System;
using System.Collections.Generic; // For List

namespace Project3.Models
{
    /// <summary>
    /// ViewModel for holding search criteria from the form.
    /// Uses explicit properties style. Manual validation needed in controller.
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

        // Public properties
        public string CuisineInput { get { return _cuisineInput; } set { _cuisineInput = value; } }
        public string City { get { return _city; } set { _city = value; } }
        public string State { get { return _state; } set { _state = value; } }

        // Constructor
        public SearchCriteriaViewModel() { }
    }
}
