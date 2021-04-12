using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class UserRegistration
    {
        [Required] public string Username { get; set; }
        [Required] [EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
    }
}