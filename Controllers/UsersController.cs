using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseWiki.Misc;
using CourseWiki.Models;
using CourseWiki.Models.DTOs.Requests;
using CourseWiki.Models.DTOs.Responses;
using CourseWiki.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace CourseWiki.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IAccountService _accountService;
        private readonly UserManager<Account> _userManager;

        public UsersController(ApiDbContext context, IAccountService accountService, UserManager<Account> userManager)
        {
            _context = context;
            _accountService = accountService;
            _userManager = userManager;
        }

        [HttpPost("authenticate")]
        public async Task<ActionResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model)
        {
            var response = await _accountService.Authenticate(model, IpAddress());
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthenticateResponse>> RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            var refreshToken = refreshTokenRequest.RefreshToken;
            var response = await _accountService.RefreshToken(refreshToken, IpAddress());
            return Ok(response);
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken(RevokeTokenRequest model)
        {
            var account_req = await _userManager.FindByIdAsync(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value);
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];
            var account = await _userManager.FindByEmailAsync(model.Email);

            if (string.IsNullOrEmpty(token))
                return BadRequest(new {message = "Token is required"});

            // users can revoke their own tokens and admins can revoke any tokens
            if (!account_req.OwnsToken(token) &&
                (await _userManager.GetRolesAsync(account_req)).Contains(Roles.Admin.ToString()) != true)
                return Unauthorized(new {message = "Unauthorized"});

            await _accountService.RevokeToken(token, IpAddress());
            return Ok(new {message = "Token revoked"});
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            await _accountService.Register(model, Request.Headers["origin"]);
            return Ok(new {message = "Registration successful, please check your email for verification instructions"});
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest model)
        {
            await _accountService.VerifyEmail(model.Email, model.Token);
            return Ok(new {message = "Verification successful, you can now login"});
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
        {
            await _accountService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new {message = "Please check your email for password reset instructions"});
        }

        [HttpPost("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken(ValidateResetTokenRequest model)
        {
            await _accountService.ValidateResetToken(model);
            return Ok(new {message = "Token is valid"});
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            await _accountService.ResetPassword(model);
            return Ok(new {message = "Password reset successful, you can now login"});
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAll()
        {
            var accounts = await _accountService.GetAll();
            return Ok(accounts);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponse>> GetById(Guid id)
        {
            var id_of_account = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var account = await _userManager.FindByIdAsync(id_of_account);
            // users can get their own account and admins can get any account
            if (id != account.Id &&
                (await _userManager.GetRolesAsync(account)).Contains(Roles.Admin.ToString()) != true)
                return Unauthorized(new {message = "Unauthorized"});

            var result = await _accountService.GetById(id);
            return Ok(result);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> Create(CreateRequest model)
        {
            var account = await _accountService.Create(model);
            return Ok(account);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<AccountResponse>> Update(Guid id, UpdateRequest model)
        {
            var account = await _userManager.FindByIdAsync(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value);
            // users can update their own account and admins can update any account
            if (id != account.Id &&
                (await _userManager.GetRolesAsync(account)).Contains(Roles.Admin.ToString()) != true)
                return Unauthorized(new {message = "Unauthorized"});

            // only admins can update role
            if ((await _userManager.GetRolesAsync(account)).Contains(Roles.Admin.ToString()) != true)
                model.Role = null;

            var status = await _accountService.Update(id, model);
            return Ok(status);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var account = await _userManager.FindByIdAsync(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value);
            // users can delete their own account and admins can delete any account

            if (id != account.Id &&
                (await _userManager.GetRolesAsync(account)).Contains(Roles.Admin.ToString()) != true)
                return Unauthorized(new {message = "Unauthorized"});

            await _accountService.Delete(id);
            return Ok(new {message = "Account deleted successfully"});
        }

        // helper methods

        private string IpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.ToString();
        }
    }
}