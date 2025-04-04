using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API to perform the password reset.
    /// </summary>
    public class ResetPasswordRequestDto
    {
        [Required]
        public string UserId { get; set; } // Consider using email/username instead? Or keep as is.

        [Required]
        public string Token { get; set; } // The reset token from the email link

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } // API will hash this
    }
}
