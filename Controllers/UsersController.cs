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
            if (response.ResponseCode == 200)return Ok(response);
            return Unauthorized(new {message = response.Message});
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthenticateResponse>> RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            var refreshToken = refreshTokenRequest.RefreshToken;
            var response = await _accountService.RefreshToken(refreshToken, IpAddress());
            if (response.ResponseCode == 200)return Ok(response);
            return BadRequest(new {message = "Refresh token failed."});
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken(RevokeTokenRequest model)
        {
            var account_req = await _userManager.FindByIdAsync(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value);
            // accept token from request body
            var token = model.Token;
            var account = await _userManager.FindByEmailAsync(model.Email);

            if (string.IsNullOrEmpty(token))
                return BadRequest(new {message = "Token is required"});

            // users can revoke their own tokens and admins can revoke any tokens
            if (!account_req.OwnsToken(token) &&
                (await _userManager.GetRolesAsync(account_req)).Contains(Roles.Admin.ToString()) != true)
                return Unauthorized(new {message = "Unauthorized"});

            var status = await _accountService.RevokeToken(token, IpAddress());
            if (status == 200)return Ok(new {message = "Token revoked"});
            return BadRequest(new {message = "Revoke token failed"});
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            int statuscode = await _accountService.Register(model, Request.Headers["origin"]);
            if (statuscode == 200)
            {
                return Ok(new
                    {message = "Registration successful, please check your email for verification instructions"});
            }

            if (statuscode == 1)
            {
                return Conflict(new
                    {message = "You email is already registered"});
            }

            if (statuscode == 2)
            {
                return BadRequest(new {message = "You password is too weak."});
            }

            return BadRequest(new {message = "Unknown error!"});
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest model)
        {
            var status = await _accountService.VerifyEmail(model.Email, model.Token);
            if (status == 200) return Ok(new {message = "Verification successful, you can now login"});
            return BadRequest(new {message = "Email verify failed, please try again."});
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
            var status = await _accountService.ValidateResetToken(model);
            if (status == 200) return Ok(new {message = "Token is valid"});
            return BadRequest(new {message = "Token is invalid"});
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            var status = await _accountService.ResetPassword(model);
            if (status == 200)return Ok(new {message = "Password reset successful, you can now login"});
            return BadRequest(new {message = "Password validation failed."});
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
            if (account.ResponseCode == 200 )return Ok(account);
            return BadRequest(new {message = account.Message});
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