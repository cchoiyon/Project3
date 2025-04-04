using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Models.DTOs
{
    /// <summary>
    /// Data returned from the API after a successful login attempt.
    /// Used by AccountController to establish the user's session/cookie.
    /// </summary>
    public class LoginResponseDto
    {
        public bool IsAuthenticated { get; set; } = false; // Should always be true if returned with 200 OK
        public bool IsVerified { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; } // Include email for potential use
        public string Role { get; set; }
        // Optionally include a JWT token if using token-based auth instead of cookies
        // public string Token { get; set; }
    }
}
