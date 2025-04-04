﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API when registering a new user.
    /// </summary>
    public class RegisterRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)] // Match RegisterModel validation
        public string Password { get; set; } // API should hash this

        [Required]
        public string UserRole { get; set; } // "reviewer" or "restaurantRep"

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [StringLength(255)]
        public string SecurityQuestion1 { get; set; }
        [Required]
        public string SecurityAnswerHash1 { get; set; } // Controller hashes before sending

        [Required]
        [StringLength(255)]
        public string SecurityQuestion2 { get; set; }
        [Required]
        public string SecurityAnswerHash2 { get; set; } // Controller hashes before sending

        [Required]
        [StringLength(255)]
        public string SecurityQuestion3 { get; set; }
        [Required]
        public string SecurityAnswerHash3 { get; set; } // Controller hashes before sending
    }
}
