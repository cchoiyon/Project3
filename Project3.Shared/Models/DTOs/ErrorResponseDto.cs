using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Standard structure for returning error messages from the API.
    /// This is a simple data container class (DTO).
    /// </summary>
    public class ErrorResponseDto // Does NOT inherit from Controller
    {
        // Property to hold the error message
        public string Message { get; set; }

        // Optionally add more details like ErrorCode, FieldErrors etc.
        // public string ErrorCode { get; set; }
        // public Dictionary<string, string[]> FieldErrors { get; set; }

        // Constructor to easily create an error response
        public ErrorResponseDto(string message)
        {
            Message = message;
        }

        // Parameterless constructor might be needed for some deserialization scenarios
        public ErrorResponseDto() { }

        // Removed Index() method - DTOs don't have controller actions
        // public IActionResult Index() { return View(); }
    }
}
