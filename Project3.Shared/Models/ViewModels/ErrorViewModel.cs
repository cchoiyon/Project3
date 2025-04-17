using System; // Included for basic types, although not strictly needed for this simple model

namespace Project3.Shared.Models.ViewModels // Updated namespace to match project structure
{
    /// <summary>
    /// Model for error display, used with the 'Error' view.
    /// </summary>
    public class ErrorViewModel
    {
        // Private backing fields
        private string _requestId;
        private bool _showRequestId;
        private string _errorMessage;
        
        // Properties with explicit getters/setters
        public string RequestId 
        { 
            get { return _requestId; } 
            set { _requestId = value; } 
        }
        
        public bool ShowRequestId 
        { 
            get { return _showRequestId; } 
            set { _showRequestId = value; } 
        }
        
        public string ErrorMessage 
        { 
            get { return _errorMessage; } 
            set { _errorMessage = value; } 
        }

        // Parameterless constructor
        public ErrorViewModel()
        {
            // Initialize properties if necessary
            // RequestId = string.Empty; // Or null by default
        }
    }
}
