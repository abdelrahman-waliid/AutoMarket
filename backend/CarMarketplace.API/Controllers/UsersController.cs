using CarMarketplace.Domain.Enums;
using CarMarketplace.API.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Controller for managing user operations including registration, authentication, and user management.
/// Delegates all logic to UserService (Clean Architecture: controller only coordinates).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IRefreshTokenCookieService _refreshTokenCookieService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">The user service.</param>
    /// <param name="refreshTokenCookieService">Cookie helper for refresh token transport.</param>
    public UsersController(IUserService userService, IRefreshTokenCookieService refreshTokenCookieService)
    {
        _userService = userService;
        _refreshTokenCookieService = refreshTokenCookieService;
    }

    /// <summary>
    /// Registers a new user in the system. Role is always set to User; only Admin can assign roles via update.
    /// </summary>
    /// <param name="request">The registration request (FullName, Email, Password). Role is ignored if sent.</param>
    /// <returns>The registered user.</returns>
    /// <response code="201">Returns the newly registered user.</response>
    /// <response code="400">If the request data is invalid or email already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDTO>> Register([FromBody] RegisterRequestDTO request)
    {
        try
        {
            var user = await _userService.RegisterAsync(
                request.FullName,
                request.Email,
                request.Password,
                UserRole.User);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>The authenticated user and JWT token.</returns>
    /// <response code="200">Returns the authenticated user and JWT token.</response>
    /// <response code="403">If the account is currently locked.</response>
    /// <response code="401">If the credentials are invalid.</response>
    /// <response code="400">If the request data is invalid.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            return StatusCode(
                StatusCodes.Status403Forbidden,
                $"Account is locked until {ex.LockoutEnd:O}.");
        }
    }

    /// <summary>
    /// Retrieves all users from the system.
    /// </summary>
    /// <returns>A list of all users.</returns>
    /// <response code="200">Returns the list of users.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDTO>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>The user if found.</returns>
    /// <response code="200">Returns the requested user.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    /// <summary>
    /// Updates an existing user. Only Admin can change a user's role (enforced in service layer).
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="userDto">The updated user data.</param>
    /// <returns>The updated user.</returns>
    /// <response code="200">Returns the updated user.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="400">If the user data is invalid or IDs do not match.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not allowed to update the target profile.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDTO>> UpdateUser(Guid id, [FromBody] UserDTO userDto)
    {
        if (id != userDto.Id)
        {
            return BadRequest("User ID in the URL does not match the ID in the request body.");
        }

        if (string.IsNullOrWhiteSpace(userDto.FullName))
        {
            return BadRequest("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(userDto.Email))
        {
            return BadRequest("Email is required.");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized("Authentication is required.");
        }

        var currentUserRole = GetCurrentUserRole();
        if (!currentUserRole.HasValue)
        {
            return Unauthorized("Authentication role is missing or invalid.");
        }

        if (currentUserRole.Value != UserRole.Admin && currentUserId.Value != id)
        {
            return Forbid();
        }

        try
        {
            var updatedUser = await _userService.UpdateUserAsync(userDto, currentUserRole.Value, currentUserId.Value);
            if (updatedUser == null)
            {
                return NotFound();
            }
            return Ok(updatedUser);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ForbiddenAccessException)
        {
            return Forbid();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Gets the current user's role from JWT claims.
    /// </summary>
    private UserRole? GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role) ? role : null;
    }

    /// <summary>
    /// Gets the current user's id from JWT claims.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Deletes a user by their unique identifier. Only Admin can delete users.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <returns>No content if the user was deleted successfully.</returns>
    /// <response code="204">If the user was deleted successfully.</response>
    /// <response code="403">If the user is not Admin.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
