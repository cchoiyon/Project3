using System; // Required for [Serializable]

namespace Project3.Models.InputModels // Ensure this namespace matches your project
{
    /// <summary>
    /// Model used for the Forgot Username view to capture user input.
    /// NOTE: Uses explicit properties with private backing fields as requested.
    /// Manual validation (required field, valid email format) is still required
    /// in the Controller action.
    /// </summary>
    [Serializable] // Added to match requested style
    public class ForgotUsernameModel
    {
        // Private backing field
        private string _email;

        // Public property with explicit get/set
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        // Parameterless constructor (matches requested style)
        public ForgotUsernameModel() { }

        // Optional: Parameterized constructor (matches requested style)
        public ForgotUsernameModel(string email)
        {
            _email = email;
        }
    }
}
