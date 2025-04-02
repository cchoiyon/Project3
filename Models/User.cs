using System;

namespace Project3.Models
{
    [Serializable]
    public class User
    {

        private int _userID;
        private string _username;
        private string _passwordHash;
        private string _email;
        private string _userType;


        public int UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string UserType
        {
            get { return _userType; }
            set { _userType = value; }
        }


        public User()
        {
        }


        public User(string username, string passwordHash, string email, string userType)
        {
            _username = username;
            _passwordHash = passwordHash;
            _email = email;
            _userType = userType;
        }
    }
}