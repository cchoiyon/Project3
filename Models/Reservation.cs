using System;

namespace Project3.Models
{
    [Serializable]
    public class Reservation
    {

        private int _reservationID;
        private int _restaurantID;
        private int _userID;
        private DateTime _reservationDateTime;
        private int _partySize;
        private string _contactName;
        private string _phone;
        private string _email;
        private string _specialRequests;
        private DateTime _createdDate;


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

        public int UserID
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

        public string ContactName
        {
            get { return _contactName; }
            set { _contactName = value; }
        }

        public string Phone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string SpecialRequests
        {
            get { return _specialRequests; }
            set { _specialRequests = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }


        public Reservation()
        {
        }


        public Reservation(int restaurantID, int userID, DateTime reservationDateTime, int partySize,
                           string contactName, string phone, string email, string specialRequests)
        {
            _restaurantID = restaurantID;
            _userID = userID;
            _reservationDateTime = reservationDateTime;
            _partySize = partySize;
            _contactName = contactName;
            _phone = phone;
            _email = email;
            _specialRequests = specialRequests;
            _createdDate = DateTime.Now;
        }
    }
}