using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class AuthenticateRequest
    {
        [Required] [EmailAddress] public string Email { get; set; }

        [Required] public string Password { get; set; }
    }
}