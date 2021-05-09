using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    /// <summary>
    /// APIs for user management.
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IAccountService _accountService;
        private readonly UserManager<Account> _userManager;

        /// <summary>
        /// Load dbcontext, account service, and user manager of .Net core Identity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="accountService"></param>
        /// <param name="userManager"></param>
        public UsersController(ApiDbContext context, IAccountService accountService, UserManager<Account> userManager)
        {
            _context = context;
            _accountService = accountService;
            _userManager = userManager;
        }

        /// <summary>
        /// Login into the system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result.</returns>
        /// <response code="200">Returns User information with jwt token and refresh token.</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="401">Username or password is invalid.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(MessageResponse), 401)]
        [HttpPost("authenticate")]
        public async Task<ActionResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model)
        {
            var response = await _accountService.Authenticate(model, IpAddress());
            if (response.ResponseCode == 200) return Ok(response);
            return Unauthorized(new {message = response.Message});
        }

        /// <summary>
        /// Fetch new jwt token by refresh token.
        /// </summary>
        /// <param name="refreshTokenRequest"></param>
        /// <returns>User information with new jwt token and refresh token.</returns>
        /// <response code="200">Returns User information with new jwt token and refresh token.</response>
        /// <response code="400">Request is invalid or token is invalid.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageResponse), 400)]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthenticateResponse>> RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            var refreshToken = refreshTokenRequest.RefreshToken;
            var response = await _accountService.RefreshToken(refreshToken, IpAddress());
            if (response.ResponseCode == 200) return Ok(response);
            return BadRequest(new {message = "Refresh token failed."});
        }

        /// <summary>
        /// Revoke refresh token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result</returns>
        /// <response code="200">Revoke succeed.</response>
        /// <response code="400">Request is invalid or revoke is failed.</response>
        /// <response code="401">jwt token is invalid.</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(MessageResponse), 400)]
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
            if (status == 200) return Ok(new {message = "Token revoked"});
            return BadRequest(new {message = "Revoke token failed"});
        }

        /// <summary>
        /// Register user.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result</returns>
        /// <response code="200">Registration succeed.</response>
        /// <response code="400">Password is weak or request is invalid.</response>
        /// <response code="409">Email is already registered</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(MessageResponse), 400)]
        [ProducesResponseType(typeof(MessageResponse), 409)]
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

        /// <summary>
        /// Verify email that user provided during register.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result</returns>
        /// <response code="200">Verification successfully.</response>
        /// <response code="400">Email verify failed or request is invalid.</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(MessageResponse), 400)]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest model)
        {
            var status = await _accountService.VerifyEmail(model.Email, model.Token);
            if (status == 200) return Ok(new {message = "Verification successful, you can now login"});
            return BadRequest(new {message = "Email verify failed, please try again."});
        }

        /// <summary>
        /// Forget password request.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result</returns>
        /// <response code="200">Request successfully.</response>
        /// <response code="400">Request is invalid.</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(ValidationResult), 400)]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
        {
            await _accountService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new {message = "Please check your email for password reset instructions"});
        }

        /// <summary>
        /// Verify password reset token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result</returns>
        /// <response code="200">Token is valid.</response>
        /// <response code="400">Token is invalid or request is invalid.</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(MessageResponse), 400)]
        [HttpPost("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken(ValidateResetTokenRequest model)
        {
            var status = await _accountService.ValidateResetToken(model);
            if (status == 200) return Ok(new {message = "Token is valid"});
            return BadRequest(new {message = "Token is invalid"});
        }

        /// <summary>
        /// Verify password reset token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Operation result</returns>
        /// <response code="200">Password reset successful.</response>
        /// <response code="400">Password validation failed or request is invalid.</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(MessageResponse), 400)]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            var status = await _accountService.ResetPassword(model);
            if (status == 200) return Ok(new {message = "Password reset successful, you can now login"});
            return BadRequest(new {message = "Password validation failed."});
        }

        /// <summary>
        /// (Admin feature) Get all user information.
        /// </summary>
        /// <returns>a list of user objects</returns>
        /// <response code="200">a list of User objects</response>
        [ProducesResponseType(typeof(AccountResponse),200)]
        [Authorize(Policy = "RequireAdmin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAll()
        {
            var accounts = await _accountService.GetAll();
            return Ok(accounts);
        }

        /// <summary>
        /// Get User object by its uuid.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>a User object</returns>
        /// <response code="200">Returns a user object</response>
        /// <response code="400">Request is invalid.</response>
        /// <response code="401">Jwt token is invalid.</response>
        /// <response code="404">Can't find user from the uuid in response.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 404)]
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

        /// <summary>
        /// Get user information themselves.
        /// </summary>
        /// <returns>A user object</returns>
        /// <response code="200">A user object</response>
        /// <response code="401">Jwt token is invalid.</response>
        [ProducesResponseType(typeof(AccountResponse),200)]
        [Authorize]
        [HttpGet("self")]
        public async Task<ActionResult<AccountResponse>> GetSelf()
        {
            var id_of_account = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var account = await _userManager.FindByIdAsync(id_of_account);

            var result = await _accountService.ToAccountResponse(account);
            return Ok(result);
        }

        /// <summary>
        /// (Admin feature) Create User.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Response of create user request.</returns>
        /// <response code="200">Create new user succeed.</response>
        /// <response code="400">Create user failed.</response>
        /// <response code="401">Jwt token is invalid.</response>
        [ProducesResponseType(typeof(AccountResponse),200)]
        [ProducesResponseType(typeof(MessageResponse),400)]
        [Authorize(Policy = "RequireAdmin")]
        [HttpPost]
        public async Task<ActionResult<AccountResponse>> Create(CreateRequest model)
        {
            var account = await _accountService.Create(model);
            if (account.ResponseCode == 200) return Ok(account);
            return BadRequest(new {message = account.Message});
        }

        /// <summary>
        /// Update user information.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns>Response of Update Request</returns>
        /// <response code="200">Update user information succeed.</response>
        /// <response code="400">Wrong password or invalid request.</response>
        /// <response code="401">Jwt token is invalid or permission denied.</response>
        [ProducesResponseType(typeof(AccountResponse),200)]
        [ProducesResponseType(typeof(MessageResponse),400)]
        [ProducesResponseType(typeof(MessageResponse),401)]
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
            if (((await _userManager.GetRolesAsync(account)).Contains(Roles.Admin.ToString()) != true) &&
                (await _userManager.CheckPasswordAsync(account, model.OldPassword)) != true)
            {
                return BadRequest(new {message = "Wrong Password!"});
            }

            var status = await _accountService.Update(id, model);
            if (status.ResponseCode == 200) return Ok(new {message = status.Message});
            return BadRequest(new {message = status.Message});
        }
        
        /// <summary>
        /// Delete user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Response of Delete Request</returns>
        /// <response code="200">Delete user successfully.</response>
        /// <response code="400">Invalid request.</response>
        /// <response code="401">Jwt token is invalid or permission denied.</response>
        [ProducesResponseType(typeof(MessageResponse),200)]
        [ProducesResponseType(typeof(MessageResponse),401)]
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