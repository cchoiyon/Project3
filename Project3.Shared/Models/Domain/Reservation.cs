namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class Reservation
    {
        // Private backing fields
        private int _reservationID;
        private int _restaurantID;
        private int? _userID;
        private DateTime _reservationDateTime;
        private int _partySize;
        private string? _contactName; 
        private string? _phone; 
        private string? _email; 
        private string? _specialRequests; 
        private string _status; 
        private DateTime _createdDate;

        // Public Properties
        public int ReservationID
        {
            get { return _reservationID; }
            set { _reservationID = value; }
        }

        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        
        public int? UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        public DateTime ReservationDateTime
        {
            get { return _reservationDateTime; }
            set { _reservationDateTime = value; }
        }

        public int PartySize
        {
            get { return _partySize; }
            set { _partySize = value; }
        }

        public string? ContactName 
        {
            get { return _contactName; }
            set { _contactName = value; }
        }

        public string? Phone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        public string? Email 
        {
            get { return _email; }
            set { _email = value; }
        }

        public string? SpecialRequests 
        {
            get { return _specialRequests; }
            set { _specialRequests = value; }
        }

       
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
           
            set { _createdDate = value; }
        }


        // Parameterless constructor
        public Reservation()
        {
            // Initialize default values if needed
            _status = "Pending"; // Default status
            _createdDate = DateTime.Now; // Set default creation date
        }


        // Parameterized constructor (Updated)
        public Reservation(int restaurantID, int? userID, DateTime reservationDateTime, int partySize,
                           string contactName, string phone, string email, string? specialRequests, string status = "Pending")
        {
            _restaurantID = restaurantID;
            _userID = userID; // Assign nullable int?
            _reservationDateTime = reservationDateTime;
            _partySize = partySize;
            _contactName = contactName;
            _phone = phone;
            _email = email;
            _specialRequests = specialRequests;
            _status = status; // Assign status
            _createdDate = DateTime.Now; // Set creation date
        }
    }
}
