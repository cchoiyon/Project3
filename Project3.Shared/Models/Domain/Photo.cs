namespace Project3.Shared.Models.Domain
{
   
    [Serializable]
    public class Photo
    {
        // Private backing fields
        private int _photoID;
        private int _restaurantID;
        private string _photoURL;
        private string _caption;
        private DateTime _uploadedDate;

        // Public Properties
        public int PhotoID
        {
            get { return _photoID; }
            set { _photoID = value; }
        }

        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        public string PhotoURL // Stores the relative or absolute URL to the image
        {
            get { return _photoURL; }
            set { _photoURL = value; }
        }

        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        public DateTime UploadedDate
        {
            get { return _uploadedDate; }
            set { _uploadedDate = value; }
        }

        // Parameterless constructor
        public Photo() { }

        // Optional: Parameterized constructor
        public Photo(int restaurantID, string photoURL, string caption)
        {
            _restaurantID = restaurantID;
            _photoURL = photoURL;
            _caption = caption;
            _uploadedDate = DateTime.Now; // Set on creation
        }
    }
}
