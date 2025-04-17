using System; // Included for basic types, although not strictly needed for this simple model

namespace Project3.Models.ViewModels // Ensure this namespace matches your project
{
    /// <summary>
    /// Standard model used by the default Error view (Views/Shared/Error.cshtml)
    /// to display error information like the Request ID.
    /// </summary>
    public class ErrorViewModel
    {
        // Using standard auto-implemented properties here as this is typical
        // for this specific template-provided model. Adjust if needed.
        public string? RequestId { get; set; } // Nullable string

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // Parameterless constructor
        public ErrorViewModel()
        {
            // Initialize properties if necessary
            // RequestId = string.Empty; // Or null by default
        }
    }
}
