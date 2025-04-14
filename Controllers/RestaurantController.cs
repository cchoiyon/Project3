using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json; // For GetFromJsonAsync
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Project3.Models.Domain; // For Restaurant, Photo
using Project3.Models.ViewModels; // For RestaurantDetailViewModel, ReviewViewModel, SearchCriteriaViewModel
// using Project3.Models.DTOs; // Add if using DTOs for API calls
using System; // For Exception, Math
using System.Linq; // For Any(), Average()
using Project3.Models.InputModels;
using System.Threading.Channels;
using System.Xml.Linq;
using Project3.Utilities;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory; // Add for caching
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Project3.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ILogger<RestaurantController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Connection _dbConnect;
        private readonly IMemoryCache _cache; // Add cache field
        private const string CuisinesCacheKey = "AllCuisines"; // Cache key for cuisines

        public RestaurantController(
            ILogger<RestaurantController> logger, 
            IHttpClientFactory httpClientFactory, 
            Connection dbConnect,
            IMemoryCache cache) // Add cache to constructor
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbConnect = dbConnect;
            _cache = cache;
        }

        // GET: /Restaurant/Details/5
        // Fetches profile, reviews, and photos from database to display details.
        public IActionResult Details(int id) // id is RestaurantID
        {
            if (id <= 0)
            {
                _logger.LogWarning("Details requested with invalid ID: {RestaurantId}", id);
                return NotFound(); // Return 404 for invalid ID
            }

            try
            {
                // Get restaurant details using stored procedure
                var cmd = new SqlCommand("dbo.TP_spGetRestaurantDetails");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds?.Tables[0]?.Rows.Count == 0)
                {
                    _logger.LogWarning("Restaurant not found with ID: {RestaurantId}", id);
                    return NotFound();
                }

                var row = ds.Tables[0].Rows[0];
                var restaurant = new Restaurant
                {
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Address = row["Address"]?.ToString() ?? string.Empty,
                    City = row["City"]?.ToString() ?? string.Empty,
                    State = row["State"]?.ToString() ?? string.Empty,
                    ZipCode = row["ZipCode"]?.ToString() ?? string.Empty,
                    Cuisine = row["Cuisine"]?.ToString() ?? string.Empty,
                    Hours = row["Hours"]?.ToString() ?? string.Empty,
                    Contact = row["Contact"]?.ToString() ?? string.Empty,
                    MarketingDescription = row["MarketingDescription"]?.ToString() ?? string.Empty,
                    WebsiteURL = row["WebsiteURL"]?.ToString() ?? string.Empty,
                    SocialMedia = row["SocialMedia"]?.ToString() ?? string.Empty,
                    Owner = row["Owner"]?.ToString() ?? string.Empty,
                    ProfilePhoto = row["ProfilePhoto"]?.ToString() ?? string.Empty,
                    LogoPhoto = row["LogoPhoto"]?.ToString() ?? string.Empty
                };

                // Get reviews using stored procedure
                var reviewCmd = new SqlCommand("dbo.TP_spGetRestaurantReviews");
                reviewCmd.CommandType = CommandType.StoredProcedure;
                reviewCmd.Parameters.AddWithValue("@RestaurantID", id);
                var reviewDs = _dbConnect.GetDataSetUsingCmdObj(reviewCmd);
                var reviews = new List<ReviewViewModel>();

                if (reviewDs?.Tables[0]?.Rows != null)
                {
                    foreach (DataRow reviewRow in reviewDs.Tables[0].Rows)
                    {
                        reviews.Add(new ReviewViewModel
                        {
                            ReviewID = Convert.ToInt32(reviewRow["ReviewID"]),
                            RestaurantID = Convert.ToInt32(reviewRow["RestaurantID"]),
                            UserID = Convert.ToInt32(reviewRow["UserID"]),
                            ReviewerUsername = reviewRow["ReviewerUsername"]?.ToString() ?? "Anonymous",
                            VisitDate = Convert.ToDateTime(reviewRow["VisitDate"]),
                            FoodQualityRating = Convert.ToInt32(reviewRow["FoodQualityRating"]),
                            ServiceRating = Convert.ToInt32(reviewRow["ServiceRating"]),
                            AtmosphereRating = Convert.ToInt32(reviewRow["AtmosphereRating"]),
                            Comments = reviewRow["Comments"]?.ToString() ?? string.Empty
                        });
                    }
                }

                // Get photos using stored procedure
                var photoCmd = new SqlCommand("dbo.TP_spGetRestaurantPhotos");
                photoCmd.CommandType = CommandType.StoredProcedure;
                photoCmd.Parameters.AddWithValue("@RestaurantID", id);
                var photoDs = _dbConnect.GetDataSetUsingCmdObj(photoCmd);
                var photos = new List<Photo>();

                if (photoDs?.Tables[0]?.Rows != null)
                {
                    foreach (DataRow photoRow in photoDs.Tables[0].Rows)
                    {
                        photos.Add(new Photo
                        {
                            PhotoID = Convert.ToInt32(photoRow["PhotoID"]),
                            RestaurantID = Convert.ToInt32(photoRow["RestaurantID"]),
                            PhotoURL = photoRow["PhotoURL"]?.ToString() ?? string.Empty,
                            Caption = photoRow["Caption"]?.ToString() ?? string.Empty
                        });
                    }
                }

                // Calculate average rating
                double averageRating = reviews.Any() ? reviews.Average(r => ((double)r.FoodQualityRating + (double)r.ServiceRating + (double)r.AtmosphereRating) / 3.0) : 0;
                string averageRatingDisplay = $"{averageRating:F1} / 5 stars";

                // Create view model
                var viewModel = new RestaurantDetailViewModel
                {
                    RestaurantID = id,
                    Profile = restaurant,
                    Reviews = reviews,
                    Photos = photos,
                    AverageRatingDisplay = averageRatingDisplay,
                    AveragePriceLevelDisplay = "$$", // This could be calculated based on actual data if available
                    AverageRating = (int)Math.Round(averageRating)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for Restaurant ID {RestaurantId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred loading restaurant details.";
                return View(new RestaurantDetailViewModel { RestaurantID = id });
            }
        }

        // Add private method to get cuisines
        private List<string> GetCuisinesList()
        {
            // Try to get cuisines from cache first
            if (_cache.TryGetValue(CuisinesCacheKey, out List<string> cachedCuisines))
            {
                return cachedCuisines;
            }

            var cuisines = new List<string>();
            try
            {
                var cmd = new SqlCommand("dbo.TP_spGetAllCuisines");
                cmd.CommandType = CommandType.StoredProcedure;
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows != null)
                {
                    cuisines = ds.Tables[0].Rows
                        .Cast<DataRow>()
                        .Where(row => row["Cuisine"] != DBNull.Value)
                        .Select(row => row["Cuisine"].ToString())
                        .Where(cuisine => !string.IsNullOrWhiteSpace(cuisine))
                        .ToList();

                    // Cache the cuisines for 1 hour
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    _cache.Set(CuisinesCacheKey, cuisines, cacheOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cuisines list");
            }

            return cuisines;
        }

        // GET: /Restaurant/Search
        public IActionResult Search()
        {
            var model = new SearchCriteriaViewModel
            {
                AvailableCuisines = GetCuisinesList()
            };
            return View(model);
        }

        // POST: /Restaurant/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search(SearchCriteriaViewModel searchCriteria)
        {
            // Initialize if null
            searchCriteria ??= new SearchCriteriaViewModel();
            
            // Always repopulate the cuisines list
            searchCriteria.AvailableCuisines = GetCuisinesList();

            try
            {
                var searchResults = new List<RestaurantViewModel>();
                var cmd = new SqlCommand("dbo.TP_spSearchRestaurants");
                cmd.CommandType = CommandType.StoredProcedure;
                
                // Add parameters only if they have values, otherwise use DBNull.Value
                cmd.Parameters.AddWithValue("@CuisineList", 
                    !string.IsNullOrWhiteSpace(searchCriteria.CuisineInput) 
                        ? searchCriteria.CuisineInput.Trim() 
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@City", 
                    !string.IsNullOrWhiteSpace(searchCriteria.City) 
                        ? searchCriteria.City.Trim() 
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@State", 
                    !string.IsNullOrWhiteSpace(searchCriteria.State) 
                        ? searchCriteria.State.Trim().ToUpper() 
                        : (object)DBNull.Value);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        string cuisine = row["Cuisine"]?.ToString() ?? string.Empty;
                        string imagePath = "/images/restaurant-placeholder.png";
                        
                        // Try to use cuisine-specific image if available
                        if (!string.IsNullOrEmpty(cuisine))
                        {
                            string cuisineImagePath = $"/images/restaurants/{cuisine.ToLower().Replace(" ", "-")}-restaurant.jpg";
                            // Check if the file exists
                            if (System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", cuisineImagePath.TrimStart('/'))))
                            {
                                imagePath = cuisineImagePath;
                            }
                        }
                        
                        // Use LogoPhoto if available, otherwise use cuisine-specific or default image
                        if (!string.IsNullOrEmpty(row["LogoPhoto"]?.ToString()))
                        {
                            imagePath = row["LogoPhoto"].ToString();
                        }
                        
                        var restaurant = new RestaurantViewModel
                        {
                            RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                            Name = row["Name"]?.ToString() ?? string.Empty,
                            Cuisine = cuisine,
                            City = row["City"]?.ToString() ?? string.Empty,
                            State = row["State"]?.ToString() ?? string.Empty,
                            Address = row["Address"]?.ToString() ?? string.Empty,
                            ProfilePhoto = imagePath,
                            AverageRating = row["OverallRating"] != DBNull.Value ? (int)Math.Round(Convert.ToDouble(row["OverallRating"])) : 0,
                            ReviewCount = row["ReviewCount"] != DBNull.Value ? Convert.ToInt32(row["ReviewCount"]) : 0,
                            AveragePriceLevel = row["AveragePriceRating"] != DBNull.Value ? (int)Math.Round(Convert.ToDouble(row["AveragePriceRating"])) : 0
                        };
                        searchResults.Add(restaurant);
                    }
                }

                // Store results in TempData to persist across redirect
                TempData["SearchResults"] = System.Text.Json.JsonSerializer.Serialize(searchResults);
                return RedirectToAction(nameof(SearchResults));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching restaurants");
                ModelState.AddModelError("", "An error occurred while searching restaurants. Please try again.");
                return View(searchCriteria);
            }
        }

        // GET: /Restaurant/SearchResults
        public IActionResult SearchResults()
        {
            var model = new SearchCriteriaViewModel
            {
                AvailableCuisines = GetCuisinesList()
            };

            if (TempData["SearchResults"] != null)
            {
                try
                {
                    ViewBag.SearchResults = System.Text.Json.JsonSerializer.Deserialize<List<RestaurantViewModel>>(
                        TempData["SearchResults"].ToString());
                    
                    // Preserve the search results for the next request since we're using them in the view
                    TempData.Keep("SearchResults");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing search results");
                    ViewBag.SearchResults = new List<RestaurantViewModel>();
                }
            }
            else
            {
                ViewBag.SearchResults = new List<RestaurantViewModel>();
            }

            return View(model);
        }

        // --- Helper methods ---
        // NOTE: Consider moving these calculation/formatting helpers into the
        // RestaurantDetailViewModel class or a dedicated utility/service class
        // to improve separation of concerns and align better with component design.

        private double CalculateAverageRating(List<ReviewViewModel> reviews)
        {
            if (reviews == null || !reviews.Any()) return 0;
            // Assuming ReviewViewModel has FoodQualityRating, ServiceRating, AtmosphereRating
            try
            {
                // Using Average() handles potential empty list implicitly if called after Any() check
                return reviews.Average(r => (r.FoodQualityRating + r.ServiceRating + r.AtmosphereRating) / 3.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating.");
                return 0; // Return default on error
            }
        }

        private int CalculateAveragePriceLevel(List<ReviewViewModel> reviews)
        {
            if (reviews == null || !reviews.Any()) return 0;
            // Assuming ReviewViewModel has PriceRating
            try
            {
                // Using Average() handles potential empty list implicitly if called after Any() check
                return (int)Math.Round(reviews.Average(r => r.PriceRating));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average price level.");
                return 0; // Return default on error
            }
        }

        private string GetStars(double rating)
        {
            // Example: Return simple text - View should handle rendering actual stars
            return $"{Math.Round(rating, 1)} / 5";
        }

        private string GetPriceLevel(int priceLevel)
        {
            // Example: Return dollar signs - View can use this directly
            priceLevel = Math.Max(1, Math.Min(5, priceLevel)); // Ensure 1-5
            return new string('$', priceLevel);
        }
        // --- End Helper Methods ---

        // Helper method to update restaurant images based on cuisine
        private void UpdateRestaurantImages()
        {
            try
            {
                // Dictionary mapping cuisine keywords to image paths
                var cuisineImageMap = new Dictionary<string, string>
                {
                    { "italian", "/images/restaurants/italian-restaurant.jpg" },
                    { "mexican", "/images/restaurants/mexican-restaurant.jpg" },
                    { "chinese", "/images/restaurants/chinese-restaurant.jpg" },
                    { "japanese", "/images/restaurants/japanese-restaurant.jpg" },
                    { "american", "/images/restaurants/american-restaurant.jpg" },
                    { "indian", "/images/restaurants/indian-restaurant.jpg" },
                    { "thai", "/images/restaurants/thai-restaurant.jpg" },
                    { "mediterranean", "/images/restaurants/mediterranean-restaurant.jpg" }
                };

                // Default image for restaurants without a matching cuisine
                string defaultImage = "/images/restaurant-placeholder.png";

                // Get all restaurants
                var cmd = new SqlCommand("SELECT RestaurantID, Cuisine FROM TP_Restaurants");
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                if (ds?.Tables[0]?.Rows != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int restaurantId = Convert.ToInt32(row["RestaurantID"]);
                        string cuisine = row["Cuisine"]?.ToString()?.ToLower() ?? string.Empty;
                        
                        // Find matching image path
                        string imagePath = defaultImage;
                        foreach (var kvp in cuisineImageMap)
                        {
                            if (cuisine.Contains(kvp.Key))
                            {
                                imagePath = kvp.Value;
                                break;
                            }
                        }
                        
                        // Update the restaurant's LogoPhoto
                        var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET LogoPhoto = @LogoPhoto WHERE RestaurantID = @RestaurantID");
                        updateCmd.Parameters.AddWithValue("@LogoPhoto", imagePath);
                        updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                        _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                    }
                }
                
                _logger.LogInformation("Restaurant images updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating restaurant images");
            }
        }

        // Action to manually update restaurant images (for testing)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateImages()
        {
            UpdateRestaurantImages();
            TempData["Message"] = "Restaurant images updated successfully";
            return RedirectToAction(nameof(Search));
        }

        // GET: /Restaurant/ManageImages/5
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ManageImages(int id)
        {
            try
            {
                // Get the restaurant details
                var cmd = new SqlCommand("SELECT * FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows.Count == 0)
                {
                    TempData["Error"] = "Restaurant not found.";
                    return RedirectToAction("Index", "Home");
                }
                
                var row = ds.Tables[0].Rows[0];
                var restaurant = new RestaurantViewModel
                {
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Cuisine = row["Cuisine"]?.ToString() ?? string.Empty,
                    City = row["City"]?.ToString() ?? string.Empty,
                    State = row["State"]?.ToString() ?? string.Empty,
                    Address = row["Address"]?.ToString() ?? string.Empty,
                    ProfilePhoto = row["ProfilePhoto"]?.ToString() ?? string.Empty,
                    LogoPhoto = row["LogoPhoto"]?.ToString() ?? string.Empty
                };
                
                return View(restaurant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading restaurant images for ID {RestaurantId}", id);
                TempData["Error"] = "An error occurred while loading restaurant images.";
                return RedirectToAction("Index", "Home");
            }
        }
        
        // POST: /Restaurant/UploadProfilePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult UploadProfilePhoto(int restaurantId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only JPG, JPEG, and PNG files are allowed.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Validate file size (max 2MB)
                if (imageFile.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 2MB.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Create directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "restaurants", restaurantId.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                // Generate unique filename
                var uniqueFileName = $"profile{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }
                
                // Update database with relative path
                var relativePath = $"/uploads/restaurants/{restaurantId}/{uniqueFileName}";
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET ProfilePhoto = @ProfilePhoto WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@ProfilePhoto", relativePath);
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Profile photo uploaded successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while uploading the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
        
        // POST: /Restaurant/UploadLogoPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult UploadLogoPhoto(int restaurantId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only JPG, JPEG, and PNG files are allowed.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Validate file size (max 2MB)
                if (imageFile.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 2MB.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Create directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "restaurants", restaurantId.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                // Generate unique filename
                var uniqueFileName = $"logo{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }
                
                // Update database with relative path
                var relativePath = $"/uploads/restaurants/{restaurantId}/{uniqueFileName}";
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET LogoPhoto = @LogoPhoto WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@LogoPhoto", relativePath);
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Logo photo uploaded successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while uploading the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
        
        // POST: /Restaurant/DeleteProfilePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult DeleteProfilePhoto(int restaurantId)
        {
            try
            {
                // Get the current profile photo path
                var cmd = new SqlCommand("SELECT ProfilePhoto FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows.Count > 0)
                {
                    var photoPath = ds.Tables[0].Rows[0]["ProfilePhoto"]?.ToString();
                    
                    // Delete the file if it exists
                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                }
                
                // Update database to remove the photo path
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET ProfilePhoto = NULL WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Profile photo deleted successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while deleting the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
        
        // POST: /Restaurant/DeleteLogoPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult DeleteLogoPhoto(int restaurantId)
        {
            try
            {
                // Get the current logo photo path
                var cmd = new SqlCommand("SELECT LogoPhoto FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows.Count > 0)
                {
                    var photoPath = ds.Tables[0].Rows[0]["LogoPhoto"]?.ToString();
                    
                    // Delete the file if it exists
                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                }
                
                // Update database to remove the photo path
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET LogoPhoto = NULL WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Logo photo deleted successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting logo photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while deleting the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
    }
}