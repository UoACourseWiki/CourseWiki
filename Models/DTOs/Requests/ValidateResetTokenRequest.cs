using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class ValidateResetTokenRequest
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string Email { get; set; }
    }
}