using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}