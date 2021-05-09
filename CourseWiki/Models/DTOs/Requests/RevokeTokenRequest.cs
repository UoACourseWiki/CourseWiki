namespace CourseWiki.Models.DTOs.Requests
{
    public class RevokeTokenRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }
}