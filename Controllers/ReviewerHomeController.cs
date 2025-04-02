using Microsoft.AspNetCore.Mvc;
using Project3.Models;    // Models namespace
using Project3.Utilities; // Utilities namespace
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization; // Needed for authorization
using System.Security.Claims; // Needed for getting UserID
using System.Collections.Generic; // Needed for List
using System; // For Math

namespace Project3.Controllers
{
    [Authorize(Roles = "reviewer")] // ONLY allow users with reviewer role
    public class ReviewerHomeController : Controller
    {
        // DB Connection obj
        private readonly DBConnect objDB = new DBConnect();

        // Main dashboard page for reviewers
        // GET: /ReviewerHome/ or /ReviewerHome/Index
        public IActionResult Index(ReviewerHomeViewModel viewModel = null) // Accept model from POST redirect
        {
            // If viewModel is null (first load), create a new one
            if (viewModel == null || viewModel.FeaturedRestaurants == null) // Check if needs loading
            {
                viewModel = new ReviewerHomeViewModel();
                // Load initial data - Featured Restaurants
                viewModel.FeaturedRestaurants = GetRestaurants(true, null); // Get featured
                viewModel.SearchResults = new List<RestaurantViewModel>(); // Init empty search results
            }

            // Load available cuisines for search filter checkboxes/dropdowns
            viewModel.AvailableCuisines = GetAvailableCuisines();

            // Get username for welcome message
            ViewData["Username"] = User.Identity.Name ?? "Reviewer"; // Get username from claims/cookie

            return View(viewModel); // Pass combined model to the view
        }

        // Handles the search form submission
        // POST: /ReviewerHome/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search(ReviewerHomeViewModel viewModel) // Bind form to SearchCriteria inside viewmodel
        {
            // Manual validation for search criteria if needed
            // e.g., check state format? city length?

            // Get search results based on criteria
            viewModel.SearchResults = GetRestaurants(false, viewModel.SearchCriteria);

            // Also need featured restaurants again for the view
            viewModel.FeaturedRestaurants = GetRestaurants(true, null);

            // Reload available cuisines
            // viewModel.AvailableCuisines = GetAvailableCuisines(); // Already done in Index call below

            // Redisplay the Index view with the search results and criteria populated
            // Pass the whole viewmodel back to the Index action which will then render the view
            return Index(viewModel);
        }

        // Helper method to get restaurants (either featured or search results)
        private List<RestaurantViewModel> GetRestaurants(bool getFeatured, SearchCriteriaViewModel criteria)
        {
            List<RestaurantViewModel> restaurantList = new List<RestaurantViewModel>();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;

            if (getFeatured)
            {
                cmd.CommandText = "dbo.TP_spGetFeaturedRestaurants"; // SP for featured
                cmd.Parameters.AddWithValue("@TopN", 6); // Get top 6
            }
            else // Search
            {
                cmd.CommandText = "dbo.TP_spSearchRestaurants"; // SP for search
                // Handle cuisine list (assuming comma separated for now)
                cmd.Parameters.AddWithValue("@CuisineList", string.IsNullOrEmpty(criteria?.CuisineInput) ? (object)DBNull.Value : criteria.CuisineInput);
                cmd.Parameters.AddWithValue("@City", string.IsNullOrEmpty(criteria?.City) ? (object)DBNull.Value : criteria.City);
                cmd.Parameters.AddWithValue("@State", string.IsNullOrEmpty(criteria?.State) ? (object)DBNull.Value : criteria.State);
            }

            DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    // Manual mapping from DataRow to ViewModel
                    RestaurantViewModel restaurant = new RestaurantViewModel();
                    restaurant.RestaurantID = Convert.ToInt32(dr["RestaurantID"]);
                    restaurant.Name = dr["Name"].ToString();
                    restaurant.Cuisine = dr["Cuisine"].ToString();
                    restaurant.City = dr["City"].ToString();
                    restaurant.State = dr["State"].ToString();
                    restaurant.LogoPhoto = dr["LogoPhoto"]?.ToString(); // Handle potential null
                    restaurant.OverallRating = Convert.ToDouble(dr["OverallRating"]);
                    restaurant.ReviewCount = Convert.ToInt32(dr["ReviewCount"]);
                    restaurant.AveragePriceRating = Convert.ToDouble(dr["AveragePriceRating"]);

                    restaurantList.Add(restaurant);
                }
            }

            return restaurantList;
        }


        // Helper to get distinct cuisine types for search filter
        private List<string> GetAvailableCuisines()
        {
            List<string> cuisines = new List<string>();
            try
            {
                // Use direct query here for simplicity, or create SP TP_spGetCuisineTypes
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT DISTINCT Cuisine FROM dbo.TP_Restaurants WHERE Cuisine IS NOT NULL ORDER BY Cuisine";
                DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        cuisines.Add(dr["Cuisine"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting cuisines: " + ex.Message); // Log error
            }
            return cuisines;
        }


        // Action to navigate to the Review Management page
        // GET: /ReviewerHome/ManageReviews
        public IActionResult ManageReviews()
        {
            // Assuming you have a ReviewController with an Index action for managing reviews
            return RedirectToAction("Index", "Review");
        }

        // Placeholder for Add Review button click (maybe handled client-side or via form post?)
        // Or redirects to the Review Management page in "add" mode
        // GET: /ReviewerHome/AddReview?restaurantId=123
        public IActionResult AddReview(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                // Handle invalid ID
                return RedirectToAction("Index"); // Or show error
            }
            // Redirect to Review controller's action for adding/editing, passing restaurant ID
            return RedirectToAction("Create", "Review", new { restaurantId = restaurantId });
        }

    }
}
