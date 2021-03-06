using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CourseWiki.Misc;
using CourseWiki.Models;
using CourseWiki.Models.DTOs.Requests;
using CourseWiki.Models.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CourseWiki.Services
{
    public interface IAccountService
    {
        Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress);
        Task<AuthenticateResponse> RefreshToken(string token, string ipAddress);
        Task<int> RevokeToken(string token, string ipAddress);
        Task<int> Register(RegisterRequest model, string origin);
        Task<int> VerifyEmail(string email, string token);
        Task ForgotPassword(ForgotPasswordRequest model, string origin);
        Task<int> ValidateResetToken(ValidateResetTokenRequest model);
        Task<int> ResetPassword(ResetPasswordRequest model);
        Task<IEnumerable<AccountResponse>> GetAll();
        Task<AccountResponse> GetById(Guid id);
        Task<AccountResponse> Create(CreateRequest model);
        Task<AccountResponse> Update(Guid id, UpdateRequest model);
        Task<AuthenticateResponse> ToAuthenticateResponse(Account account);
        Task<AccountResponse> ToAccountResponse(Account account);
        Task Delete(Guid id);
    }

    public class AccountService : IAccountService
    {
        private readonly ApiDbContext _context;
        private readonly UserManager<Account> _userManager;
        private readonly AppSettings _appSettings;
        private readonly IEmailService _emailService;

        public AccountService(
            ApiDbContext context,
            UserManager<Account> userManager,
            IOptions<AppSettings> appSettings,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _appSettings = appSettings.Value;
            _emailService = emailService;
        }

        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);
            if (account == null || await _userManager.IsEmailConfirmedAsync(account) == false ||
                await _userManager.CheckPasswordAsync(account, model.Password) == false)
                return new AuthenticateResponse() {ResponseCode = 401, Message = "Email or password is incorrect"};

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = await GenerateJwtToken(account);
            var refreshToken = GenerateRefreshToken(ipAddress);
            account.RefreshTokens.Add(refreshToken);

            // remove old refresh tokens from account
            RemoveOldRefreshTokens(account);
            await _userManager.UpdateAsync(account);

            var response = await ToAuthenticateResponse(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            response.ResponseCode = 200;
            response.Message = "Login Succeed!";
            return response;
        }

        public async Task<AuthenticateResponse> RefreshToken(string token, string ipAddress)
        {
            var (refreshToken, account, statusCode, message) = GetRefreshToken(token);
            if (statusCode == 401) return new AuthenticateResponse() {ResponseCode = statusCode, Message = message};
            // replace old refresh token with a new one and save
            var newRefreshToken = GenerateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            account.RefreshTokens.Add(newRefreshToken);

            RemoveOldRefreshTokens(account);
            await _userManager.UpdateAsync(account);

            // generate new jwt
            var jwtToken = await GenerateJwtToken(account);

            var response = await ToAuthenticateResponse(account);
            response.JwtToken = jwtToken;
            response.RefreshToken = newRefreshToken.Token;
            response.ResponseCode = 200;
            response.Message = "Get new token successfully.";
            return response;
        }

        public async Task<int> RevokeToken(string token, string ipAddress)
        {
            var (refreshToken, account, statusCode, message) = GetRefreshToken(token);
            if (statusCode == 400) return 400;
            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            await _userManager.UpdateAsync(account);
            return 200;
        }

        public async Task<int> Register(RegisterRequest model, string origin)
        {
            // validate
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                // send already registered error in email to prevent account enumeration
                SendAlreadyRegisteredEmail(model.Email, origin);
                return 1;
            }

            // map model to new account object
            var account = new Account
            {
                NickName = model.NickName, AcceptTerms = model.AcceptTerms, Created = DateTime.UtcNow
            };
            await _userManager.SetEmailAsync(account, model.Email);
            await _userManager.SetUserNameAsync(account, model.Email);
            // first registered account is an admin
            var isFirstAccount = _userManager.Users.Count() == 0;
            foreach (var passwordValidator in _userManager.PasswordValidators.ToList())
            {
                var result = await passwordValidator.ValidateAsync(_userManager, null, model.Password);
                if (!result.Succeeded)
                {
                    return 2;
                }
            }

            var status = await _userManager.CreateAsync(account, model.Password);
            await _userManager.AddToRoleAsync(account, isFirstAccount ? Roles.Admin.ToString() : Roles.User.ToString());
            account.Created = DateTime.UtcNow;
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(account);

            // send email
            SendVerificationEmail(account, origin, HttpUtility.UrlEncode(token));
            return 200;
        }

        public async Task<int> VerifyEmail(string email, string token)
        {
            var account = await _userManager.FindByEmailAsync(email);
            if (account == null) return 400;

            if (!(await _userManager.ConfirmEmailAsync(account, token)).Succeeded) return 400;

            account.Verified = DateTime.UtcNow;
            await _userManager.UpdateAsync(account);
            return 200;
        }

        public async Task ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);
            // always return ok response to prevent email enumeration
            if (account == null) return;

            // create reset token that expires after 1 day
            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            account.ResetTokenExpires = DateTime.UtcNow.AddDays(1);

            await _userManager.UpdateAsync(account);

            // send email
            SendPasswordResetEmail(account, origin, HttpUtility.UrlEncode(token));
        }

        public async Task<int> ValidateResetToken(ValidateResetTokenRequest model)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);

            if (account == null || account.ResetTokenExpires < DateTime.UtcNow ||
                await _userManager.VerifyUserTokenAsync(account, _userManager.Options.Tokens.PasswordResetTokenProvider,
                    "ResetPassword", model.Token)) return 401;
            return 200;
        }

        public async Task<int> ResetPassword(ResetPasswordRequest model)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);

            if (account == null)
                return 401;

            // update password and remove reset token
            account.PasswordHash = _userManager.PasswordHasher.HashPassword(account, model.Password);
            account.PasswordReset = DateTime.UtcNow;
            account.ResetTokenExpires = null;

            var status = await _userManager.UpdateAsync(account);
            return 200;
        }

        public async Task<IEnumerable<AccountResponse>> GetAll()
        {
            var accounts = _userManager.Users.ToList();
            List<AccountResponse> accountResponses = new List<AccountResponse>();
            foreach (var account in accounts)
            {
                accountResponses.Add(await ToAccountResponse(account));
            }

            return accountResponses;
        }

        public async Task<AccountResponse> GetById(Guid id)
        {
            var account = await GetAccount(id);
            return await ToAccountResponse(account);
        }

        public async Task<AccountResponse> Create(CreateRequest model)
        {
            // validate
            if (_userManager.FindByEmailAsync(model.Email) != null)
                return new AccountResponse()
                    {ResponseCode = 400, Message = $"Email '{model.Email}' is already registered"};

            var account = new Account()
            {
                NickName = model.NickName, Created = DateTime.Now,
                Verified = DateTime.UtcNow
            };

            // save account
            await _userManager.CreateAsync(account);
            await _userManager.AddPasswordAsync(account, model.Password);

            return await ToAccountResponse(account);
        }

        public async Task<AccountResponse> Update(Guid id, UpdateRequest model)
        {
            var account = await GetAccount(id);

            // validate
            if (!string.IsNullOrEmpty(model.Email) && account.Email != model.Email &&
                _userManager.FindByEmailAsync(model.Email) != null)
                return new AccountResponse() {ResponseCode = 400, Message = $"Email '{model.Email}' is already taken"};

            // hash password if it was entered
            if (!string.IsNullOrEmpty(model.Password))
                await _userManager.ChangePasswordAsync(account, model.OldPassword, model.Password);
            // copy model to account and save
            if (!string.IsNullOrEmpty(model.NickName)) account.NickName = model.NickName;
            account.Updated = DateTime.UtcNow;
            if (model.Role != null) await _userManager.AddToRoleAsync(account, model.Role);
            if ((await _userManager.UpdateAsync(account)).Succeeded)
            {
                var accountResponse = await ToAccountResponse(account);
                accountResponse.ResponseCode = 200;
                accountResponse.Message = "Update Succeed.";
                return accountResponse;
            }

            return new AccountResponse() {ResponseCode = 400, Message = $"Unknown Error!"};
        }

        public async Task Delete(Guid id)
        {
            var account = await GetAccount(id);
            await _userManager.DeleteAsync(account);
        }

        // helper methods

        private async Task<Account> GetAccount(Guid id)
        {
            var account = await _userManager.FindByIdAsync(id.ToString());
            if (account == null) throw new KeyNotFoundException("Account not found");
            return account;
        }

        private (RefreshToken, Account, int StatusCode, string Message) GetRefreshToken(string token)
        {
            var account = _userManager.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));
            if (account == null) return (null, null, 401, "Invalid token");
            var refreshToken = account.RefreshTokens.Single(x => x.Token == token);
            if (!refreshToken.IsActive) return (null, null, 401, "Invalid token");
            return (refreshToken, account, 200, "Generate Refresh Token Success");
        }

        private async Task<string> GenerateJwtToken(Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            List<Claim> claims = new List<Claim>();
            var roles = await _userManager.GetRolesAsync(account);
            claims.Add(new Claim("id", account.Id.ToString()));
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = randomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        private void RemoveOldRefreshTokens(Account account)
        {
            account.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
        }

        private string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        private void SendVerificationEmail(Account account, string origin, string token)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var verifyUrl = $"{origin}/email_validate?email={account.Email}&token={token}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                message =
                    $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                             <p><code>{token}</code></p>";
            }

            _emailService.Send(
                to: account.Email,
                subject: "Sign-up Verification API - Verify Email",
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         {message}"
            );
        }

        private void SendAlreadyRegisteredEmail(string email, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
                message =
                    $@"<p>If you don't know your password please visit the <a href=""{origin}/password_reset"">forgot password</a> page.</p>";
            else
                message =
                    $@"<p>If you don't know your password please visit the <a href=""{origin}/password_reset"">forgot password</a> page.</p>";

            _emailService.Send(
                to: email,
                subject: "Sign-up Verification API - Email Already Registered",
                html: $@"<h4>Email Already Registered</h4>
                         <p>Your email <strong>{email}</strong> is already registered.</p>
                         {message}"
            );
        }

        private void SendPasswordResetEmail(Account account, string origin, string token)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/reset?email={account.Email}&token={token}";
                message =
                    $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message =
                    $@"<p>Please use the below token to reset your password with the <code>/Users/reset-password</code> api route:</p>
                             <p><code>{token}</code></p>";
            }

            _emailService.Send(
                to: account.Email,
                subject: "Sign-up Verification API - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                         {message}"
            );
        }

        public async Task<AuthenticateResponse> ToAuthenticateResponse(Account account)
        {
            if (account != null)
            {
                return new AuthenticateResponse
                {
                    Id = account.Id,
                    Created = account.Created,
                    Email = account.Email,
                    NickName = account.NickName,
                    Roles = await _userManager.GetRolesAsync(account) as List<string>,
                    Updated = account.Updated
                };
            }

            return null;
        }

        public async Task<AccountResponse> ToAccountResponse(Account account)
        {
            if (account != null)
            {
                return new AccountResponse()
                {
                    Id = account.Id,
                    Created = account.Created,
                    Email = account.Email,
                    NickName = account.NickName,
                    Roles = await _userManager.GetRolesAsync(account) as List<string>,
                    Updated = account.Updated,
                    IsVerified = await _userManager.IsEmailConfirmedAsync(account)
                };
            }

            return null;
        }
    }
}