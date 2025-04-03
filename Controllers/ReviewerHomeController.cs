using Microsoft.AspNetCore.Mvc;
using Project3.Models;    // My models
using Project3.Utilities; // My utils
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization; // Need this for authorize
using System.Security.Claims; // Need this for UserID
using System.Collections.Generic; // Need this for List
using System; // Need this for Math, Exception

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
            // If viewModel is null (first load) or doesnt have results, create a new one and load featured
            // This handles both initial GET and returning after POST search
            bool loadFeatured = false;
            if (viewModel == null)
            {
                viewModel = new ReviewerHomeViewModel();
                loadFeatured = true; // Load featured on initial GET
            }
            else if (viewModel.SearchResults == null) // If viewModel passed but no search results (e.g., error?)
            {
                viewModel.SearchResults = new List<RestaurantViewModel>();
                loadFeatured = true; // Load featured if search results are missing
            }
            // If viewModel.SearchResults is NOT null, it means Search action populated it, so don't load featured

            if (loadFeatured)
            {
                // Load initial data - Featured Restaurants
                viewModel.FeaturedRestaurants = GetRestaurants(true, null); // Get featured
            }

            // Always load available cuisines for search filter
            viewModel.AvailableCuisines = GetAvailableCuisines();

            // Get username for welcome message
            ViewData["Username"] = User.Identity?.Name ?? "Reviewer"; // Get username from claims/cookie

            return View(viewModel); // Pass combined model to the Views/ReviewerHome/Index.cshtml view
        }

        // Handles the search form submission
        // POST: /ReviewerHome/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search(ReviewerHomeViewModel viewModel) // Bind form to SearchCriteria inside viewmodel
        {
            // Manual validation for search criteria if needed?
            // For now, assume SP handles empty/null inputs ok

            try
            {
                // Get search results based on criteria
                viewModel.SearchResults = GetRestaurants(false, viewModel.SearchCriteria);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching restaurants: {ex.Message}"); // Log error
                ViewData["SearchError"] = "An error occurred during search.";
                viewModel.SearchResults = new List<RestaurantViewModel>(); // Ensure it's empty on error
            }


            // Don't need to reload featured restaurants here, the Index view logic handles it
            // viewModel.FeaturedRestaurants = GetRestaurants(true, null);

            // Don't need to reload cuisines here, Index action will do it

            // Redisplay the Index view by calling the Index action method,
            // passing the viewModel containing search criteria and results
            return Index(viewModel);
        }

        // Helper method to get restaurants (either featured or search results)
        private List<RestaurantViewModel> GetRestaurants(bool getFeatured, SearchCriteriaViewModel criteria)
        {
            var restaurantList = new List<RestaurantViewModel>(); // list to hold results
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;

            if (getFeatured)
            {
                cmd.CommandText = "dbo.TP_spGetFeaturedRestaurants"; // SP for featured
                cmd.Parameters.AddWithValue("@TopN", 6); // Get top 6 maybe
            }
            else // Search
            {
                cmd.CommandText = "dbo.TP_spSearchRestaurants"; // SP for search
                // Handle parameters, pass DBNull if criteria is null or empty
                cmd.Parameters.AddWithValue("@CuisineList", string.IsNullOrEmpty(criteria?.CuisineInput) ? (object)DBNull.Value : criteria.CuisineInput);
                cmd.Parameters.AddWithValue("@City", string.IsNullOrEmpty(criteria?.City) ? (object)DBNull.Value : criteria.City);
                cmd.Parameters.AddWithValue("@State", string.IsNullOrEmpty(criteria?.State) ? (object)DBNull.Value : criteria.State);
            }

            DataSet ds = objDB.GetDataSetUsingCmdObj(cmd);

            // Check if dataset has results
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                // Loop through results and map to viewmodel
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    // Manual mapping from DataRow to ViewModel
                    // Need try/catch or checks around conversions? maybe later
                    RestaurantViewModel restaurant = new RestaurantViewModel();
                    restaurant.RestaurantID = Convert.ToInt32(dr["RestaurantID"]);
                    restaurant.Name = dr["Name"].ToString();
                    restaurant.Cuisine = dr["Cuisine"].ToString();
                    restaurant.City = dr["City"].ToString();
                    restaurant.State = dr["State"].ToString();
                    restaurant.LogoPhoto = dr["LogoPhoto"]?.ToString(); // Handle potential null
                    // Use safe conversion for doubles/ints
                    restaurant.OverallRating = dr["OverallRating"] != DBNull.Value ? Convert.ToDouble(dr["OverallRating"]) : 0.0;
                    restaurant.ReviewCount = dr["ReviewCount"] != DBNull.Value ? Convert.ToInt32(dr["ReviewCount"]) : 0;
                    restaurant.AveragePriceRating = dr["AveragePriceRating"] != DBNull.Value ? Convert.ToDouble(dr["AveragePriceRating"]) : 0.0;

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
                cmd.CommandText = "SELECT DISTINCT Cuisine FROM dbo.TP_Restaurants WHERE Cuisine IS NOT NULL AND LEN(Cuisine) > 0 ORDER BY Cuisine"; // Added LEN check
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
                                                                            // Return empty list or handle error
            }
            return cuisines;
        }


        // Action to navigate to the Review Management page
        // GET: /ReviewerHome/ManageReviews
        public IActionResult ManageReviews()
        {
            // Redirect to Review controller's Index action
            return RedirectToAction("Index", "Review");
        }

        // Action triggered by "Start a review" button on a restaurant card
        // GET: /ReviewerHome/AddReview?restaurantId=123
        public IActionResult AddReview(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                // Handle invalid ID maybe?
                return RedirectToAction("Index"); // Just go back home
            }
            // Redirect to Review controller's Create action, passing restaurant ID
            return RedirectToAction("Create", "Review", new { restaurantId = restaurantId });
        }

    }
}
