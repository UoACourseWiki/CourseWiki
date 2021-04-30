using System;
using System.Collections.Generic;

namespace CourseWiki.Models.DTOs.Responses
{
    public class AccountResponse
    {
        public Guid Id { get; set; }
        public string NickName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public bool IsVerified { get; set; }
    }
}