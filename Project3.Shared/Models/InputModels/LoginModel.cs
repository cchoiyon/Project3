using System; // Required for [Serializable]

namespace Project3.Shared.Models.InputModels // Updated namespace to match project structure
{
    /// <summary>
    /// Model for the Login view.
    /// NOTE: Uses explicit properties with private backing fields as requested.
    /// Manual validation is required in the Controller.
    /// </summary>
    [Serializable]
    public class LoginModel
    {
        // Private backing fields
        private string _username;
        private string _password;
        private bool _rememberMe;

        // Public Properties with explicit get/set
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public bool RememberMe
        {
            get { return _rememberMe; }
            set { _rememberMe = value; }
        }

        // Parameterless constructor
        public LoginModel() { }

        // Optional: Parameterized constructor
        public LoginModel(string username, string password, bool rememberMe)
        {
            _username = username;
            _password = password;
            _rememberMe = rememberMe;
        }
    }
}
