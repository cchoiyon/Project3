namespace Project3.Shared.Models.Configuration
{
    /// <summary>
    /// Represents SMTP configuration settings read from appsettings.json.
    /// Using explicit properties style as requested for other models.
    /// </summary>
    public class SmtpSettings
    {
        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private string _fromAddress;
        private bool _enableSsl;

        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
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
        public string FromAddress
        {
            get { return _fromAddress; }
            set { _fromAddress = value; }
        }
        public bool EnableSsl
        {
            get { return _enableSsl; }
            set { _enableSsl = value; }
        }

        // Parameterless constructor
        public SmtpSettings() { }
    }
}
