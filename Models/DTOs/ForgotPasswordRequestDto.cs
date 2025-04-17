using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API to request a password reset email.
    /// </summary>
    public class ForgotPasswordRequestDto
    {
        [Required]
        public string EmailOrUsername { get; set; }
    }
}
