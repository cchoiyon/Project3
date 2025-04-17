// Ensure this namespace matches your folder structure
namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class Review
    {
        // Private backing fields
        private int _reviewID;
        private int _restaurantID;
        private int _userID;
        private DateTime _visitDate;
        private string? _comments; // *** CHANGED: Made nullable (string?) to match DB ***
        private int _foodQualityRating;
        private int _serviceRating;
        private int _atmosphereRating;
        private int _priceRating;
        private DateTime _createdDate;

        // Public Properties
        public int ReviewID
        {
            get { return _reviewID; }
            set { _reviewID = value; }
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

        public DateTime VisitDate
        {
            get { return _visitDate; }
            set { _visitDate = value; }
        }

        // *** CHANGED: Property is now nullable string? ***
        public string? Comments // Allow null
        {
            get { return _comments; }
            set { _comments = value; }
        }

        public int FoodQualityRating
        {
            get { return _foodQualityRating; }
            set { _foodQualityRating = value; }
        }

        public int ServiceRating
        {
            get { return _serviceRating; }
            set { _serviceRating = value; }
        }

        public int AtmosphereRating
        {
            get { return _atmosphereRating; }
            set { _atmosphereRating = value; }
        }

        public int PriceRating
        {
            get { return _priceRating; }
            set { _priceRating = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            // Typically CreatedDate is set by DB default, so setter might be private or removed if not needed
            set { _createdDate = value; }
        }


        // Parameterless constructor
        public Review()
        {
            // Initialize default values if needed
            _createdDate = DateTime.Now; // Set default creation date
        }


        // Parameterized constructor (Updated)
        public Review(int restaurantID, int userID, DateTime visitDate, string? comments, // Accept nullable string
                      int foodQualityRating, int serviceRating, int atmosphereRating, int priceRating)
        {
            _restaurantID = restaurantID;
            _userID = userID;
            _visitDate = visitDate;
            _comments = comments; // Assign nullable string
            _foodQualityRating = foodQualityRating;
            _serviceRating = serviceRating;
            _atmosphereRating = atmosphereRating;
            _priceRating = priceRating;
            _createdDate = DateTime.Now; // Set creation date
        }
    }
}
