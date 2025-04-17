using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data returned FROM the API for restaurant search results or featured lists.
    /// Similar to RestaurantViewModel but specific to API contract.
    /// </summary>
    public class RestaurantSearchResultDto
    {
        public int RestaurantID { get; set; }
        public string Name { get; set; }
        public string? LogoPhoto { get; set; }
        public string? Cuisine { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public double OverallRating { get; set; }
        public int ReviewCount { get; set; }
        public double AveragePriceRating { get; set; }
    }
}
