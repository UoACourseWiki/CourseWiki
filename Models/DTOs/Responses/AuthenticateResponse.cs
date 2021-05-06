using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CourseWiki.Models.DTOs.Responses
{
    public class AuthenticateResponse
    {
        public Guid Id { get; set; }
        public string NickName { get; set; }
        public string Email { get; set; }
        [JsonIgnore]
        public string Message { get; set; }
        [JsonIgnore]
        public int ResponseCode { get; set; }
        public List<string> Roles { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public bool IsVerified { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
    }
}