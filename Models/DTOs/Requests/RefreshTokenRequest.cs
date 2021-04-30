using System.ComponentModel.DataAnnotations;

namespace CourseWiki.Models.DTOs.Requests
{
    public class RefreshTokenRequest
    {
        [Required] public string JwtToken { get; set; }

        [Required] public string RefreshToken { get; set; }
    }
}