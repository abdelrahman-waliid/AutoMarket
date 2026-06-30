using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CarMarketplace.API.Interfaces;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Enums;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Handles refresh token and logout. Does not require authentication for refresh.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string ForgotPasswordResponseMessage =
        "If an account with that email exists, password reset instructions were sent.";

    private readonly IUserService _userService;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;
    private readonly IRefreshRequestCsrfProtectionService _refreshRequestCsrfProtectionService;
    private readonly IPasswordResetEmailService _passwordResetEmailService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        IRefreshTokenCookieService refreshTokenCookieService,
        IRefreshRequestCsrfProtectionService refreshRequestCsrfProtectionService,
        IPasswordResetEmailService passwordResetEmailService,
        ICurrentUserService? currentUserService = null,
        ILogger<AuthController>? logger = null)
    {
        _userService = userService;
        _refreshTokenCookieService = refreshTokenCookieService;
        _refreshRequestCsrfProtectionService = refreshRequestCsrfProtectionService;
        _passwordResetEmailService = passwordResetEmailService;
        _currentUserService = currentUserService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>.Instance;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> Register([FromBody] RegisterRequestDTO request)
    {
        var user = await _userService.RegisterAsync(
            request.FullName,
            request.Email,
            request.Password,
            UserRole.User);

        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>
    /// Authenticates a user and issues access + refresh token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO request)
    {
        try
        {
            var result = await _userService.AuthenticateAsync(request.Email, request.Password);
            if (result == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            _refreshTokenCookieService.SetCookie(Response, result.RefreshToken);
            return Ok(result.Response);
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    /// <summary>
    /// Starts forgot-password flow.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
    {
        var normalizedEmail = request.Email.Trim();
        var resetToken = await _userService.ForgotPasswordAsync(normalizedEmail);

        if (!string.IsNullOrWhiteSpace(resetToken))
        {
            await _passwordResetEmailService.SendPasswordResetAsync(
                normalizedEmail,
                resetToken,
                HttpContext.RequestAborted);
        }

        return Ok(new { message = ForgotPasswordResponseMessage });
    }

    /// <summary>
    /// Resets password using a valid one-time token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
    {
        await _userService.ResetPasswordAsync(request.Token, request.NewPassword);
        _refreshTokenCookieService.ClearCookie(Response);
        return Ok(new { message = "Password reset successful." });
    }

    /// <summary>
    /// Changes the current authenticated user's password and revokes active sessions.
    /// </summary>
    [Authorize]
    [EnableRateLimiting("SensitiveAuth")]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request)
    {
        var userId = _currentUserService?.GetCurrentUserId();
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Unauthorized("Authentication is required.");
        }

        try
        {
            var changed = await _userService.ChangePasswordAsync(
                userId.Value,
                request.CurrentPassword,
                request.NewPassword);

            if (!changed)
            {
                return BadRequest("Current password is invalid.");
            }

            _refreshTokenCookieService.ClearCookie(Response);
            _logger.LogInformation("Password changed for user {UserId}. Active refresh tokens revoked.", userId.Value);

            return Ok(new { message = "Password changed successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access token.
    /// </summary>
    /// <returns>New access token and user, or 401 if the refresh token is invalid.</returns>
    /// <response code="200">Returns user and new access token.</response>
    /// <response code="401">If the refresh token cookie is missing, invalid, expired, or revoked.</response>
    /// <response code="403">If refresh token reuse is detected and sessions are revoked.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponseDTO>> Refresh()
    {
        if (!_refreshRequestCsrfProtectionService.IsRequestAllowed(Request))
        {
            return Forbid();
        }

        try
        {
            if (!_refreshTokenCookieService.TryGetFromRequest(Request, out var refreshToken))
            {
                return Unauthorized("Refresh token cookie is missing.");
            }

            var result = await _userService.RefreshAccessTokenAsync(refreshToken);
            if (result == null)
            {
                _refreshTokenCookieService.ClearCookie(Response);
                return Unauthorized("Invalid or expired refresh token.");
            }

            _refreshTokenCookieService.SetCookie(Response, result.RefreshToken);
            return Ok(result.Response);
        }
        catch (RefreshTokenReuseDetectedException ex)
        {
            _refreshTokenCookieService.ClearCookie(Response);
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    /// <summary>
    /// Revokes the given refresh token (logout). Client should discard the token after calling.
    /// </summary>
    /// <returns>204 if revoked or token was not found (idempotent).</returns>
    /// <response code="204">Token revoked or not found.</response>
    /// <response code="403">If CSRF origin validation fails for cross-site refresh cookies.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout()
    {
        if (!_refreshRequestCsrfProtectionService.IsRequestAllowed(Request))
        {
            return Forbid();
        }

        if (_refreshTokenCookieService.TryGetFromRequest(Request, out var refreshToken))
        {
            await _userService.RevokeRefreshTokenAsync(refreshToken);
        }

        _refreshTokenCookieService.ClearCookie(Response);
        return NoContent();
    }
}
