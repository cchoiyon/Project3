using System;

// Ensure this namespace matches your folder structure
namespace Project3.Models.Domain
{
    [Serializable]
    public class Reservation
    {
        // Private backing fields
        private int _reservationID;
        private int _restaurantID;
        private int? _userID; // *** CHANGED: Made nullable (int?) to match DB ***
        private DateTime _reservationDateTime;
        private int _partySize;
        private string? _contactName; // Made nullable string
        private string? _phone; // Made nullable string
        private string? _email; // Made nullable string
        private string? _specialRequests; // Made nullable string
        private string _status; // *** ADDED: Status property ***
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

        // *** CHANGED: Property is now nullable int? ***
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

        public string? ContactName // Allow null
        {
            get { return _contactName; }
            set { _contactName = value; }
        }

        public string? Phone // Allow null
        {
            get { return _phone; }
            set { _phone = value; }
        }

        public string? Email // Allow null
        {
            get { return _email; }
            set { _email = value; }
        }

        public string? SpecialRequests // Allow null
        {
            get { return _specialRequests; }
            set { _specialRequests = value; }
        }

        // *** ADDED: Status property ***
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            // Typically CreatedDate is set by DB default, so setter might be private or removed
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
