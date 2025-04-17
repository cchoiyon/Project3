using System; // Required for [Serializable]

namespace Project3.Shared.Models.InputModels // Updated namespace to match project structure
{
    /// <summary>
    /// Model used for the Reset Password view.
    /// NOTE: Style updated to use private backing fields and explicit properties
    /// to match the requested style. Manual validation (required fields,
    /// password match, length) is still required in the Controller action.
    /// </summary>
    [Serializable] // Added to match requested style
    public class ResetPasswordModel
    {
        // Private backing fields
        private string _userId;
        private string _token;
        private string _newPassword;
        private string _confirmPassword;

        // Public property for User ID
        public string UserId // Or int
        {
            get { return _userId; }
            set { _userId = value; }
        }

        // Public property for Token
        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }

        // Public property for New Password
        public string NewPassword
        {
            get { return _newPassword; }
            set { _newPassword = value; }
        }

        // Public property for Confirm Password
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; }
        }

        // Parameterless constructor (matches requested style)
        public ResetPasswordModel() { }

        // Optional: Parameterized constructor (matches requested style)
        public ResetPasswordModel(string userId, string token, string newPassword, string confirmPassword)
        {
            _userId = userId;
            _token = token;
            _newPassword = newPassword;
            _confirmPassword = confirmPassword;
        }
    }
}
