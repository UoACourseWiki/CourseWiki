using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class VerifyEmailRequest
    {
        [Required] public string Email { get; set; }

        [Required] public string Token { get; set; }
    }
}