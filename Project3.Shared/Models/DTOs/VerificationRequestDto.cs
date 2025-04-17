using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// DTOs for API communication
namespace Project3.Shared.Models.DTOs
{
    /// <summary>
    /// Data sent TO the API to confirm an email address.
    /// </summary>
    public class VerificationRequestDto
    {
        [Required]
        public string VerificationToken { get; set; }
    }
}
