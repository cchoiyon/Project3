using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data returned FROM the API representing a photo (alternative to Photo domain model).
    /// </summary>
    public class PhotoDto
    {
        public int PhotoID { get; set; }
        public string PhotoURL { get; set; }
        public string? Caption { get; set; }
        public DateTime UploadedDate { get; set; }
    }
}
