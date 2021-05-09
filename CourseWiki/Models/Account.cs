using System;
using System.Collections.Generic;
using CourseWiki.Models.DTOs.Responses;
using Microsoft.AspNetCore.Identity;

namespace CourseWiki.Models
{
    public class Account : IdentityUser<Guid>
    {
        public string NickName { get; set; }
        public bool AcceptTerms { get; set; }
        public DateTime? Verified { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }
    }
}