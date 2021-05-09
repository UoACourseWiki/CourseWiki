using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class ResetPasswordRequest
    {
        [Required] public string Token { get; set; }
        [Required] public string Email { get; set; }

        [Required] [MinLength(6)] public string Password { get; set; }

        [Required] [Compare("Password")] public string ConfirmPassword { get; set; }
    }
}