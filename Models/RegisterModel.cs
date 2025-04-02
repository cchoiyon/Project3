using System; // Required for [Serializable]

namespace Project3.Models // Ensure this namespace matches your project
{
    /// <summary>
    /// Model for the Register view.
    /// NOTE: Uses explicit properties with private backing fields as requested.
    /// Manual validation is required in the Controller. Includes Security Q/A.
    /// </summary>
    [Serializable]
    public class RegisterModel
    {
        // Private backing fields
        private string _username;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _userType; // "reviewer" or "restaurantRep"
        private bool _rememberLoginId;
        // Private fields for Security Questions/Answers
        private string _securityQuestion1;
        private string _securityAnswer1; // Store plain text here, hash in Controller/SP
        private string _securityQuestion2;
        private string _securityAnswer2; // Store plain text here, hash in Controller/SP
        private string _securityQuestion3;
        private string _securityAnswer3; // Store plain text here, hash in Controller/SP


        // Public Properties with explicit get/set
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; }
        }

        public string UserType
        {
            get { return _userType; }
            set { _userType = value; }
        }

        public bool RememberLoginId
        {
            get { return _rememberLoginId; }
            set { _rememberLoginId = value; }
        }

        // Public properties for Security Questions/Answers
        public string SecurityQuestion1
        {
            get { return _securityQuestion1; }
            set { _securityQuestion1 = value; }
        }

        // NOTE: Controller should HASH this before storing/validating
        public string SecurityAnswer1
        {
            get { return _securityAnswer1; }
            set { _securityAnswer1 = value; }
        }

        public string SecurityQuestion2
        {
            get { return _securityQuestion2; }
            set { _securityQuestion2 = value; }
        }

        // NOTE: Controller should HASH this before storing/validating
        public string SecurityAnswer2
        {
            get { return _securityAnswer2; }
            set { _securityAnswer2 = value; }
        }

        public string SecurityQuestion3
        {
            get { return _securityQuestion3; }
            set { _securityQuestion3 = value; }
        }

        // NOTE: Controller should HASH this before storing/validating
        public string SecurityAnswer3
        {
            get { return _securityAnswer3; }
            set { _securityAnswer3 = value; }
        }


        // Parameterless constructor
        public RegisterModel()
        {
            // Set default UserType if desired
            // this._userType = "reviewer";
        }

        // Optional: Parameterized constructor
        public RegisterModel(string username, string email, string password, string confirmPassword, string userType, bool rememberLoginId,
                             string sq1, string sa1, string sq2, string sa2, string sq3, string sa3)
        {
            this._username = username;
            this._email = email;
            this._password = password;
            this._confirmPassword = confirmPassword;
            this._userType = userType;
            this._rememberLoginId = rememberLoginId;
            this._securityQuestion1 = sq1;
            this._securityAnswer1 = sa1;
            this._securityQuestion2 = sq2;
            this._securityAnswer2 = sa2;
            this._securityQuestion3 = sq3;
            this._securityAnswer3 = sa3;
        }
    }
}
