using CarMarketplace.API.DTOs;
using CarMarketplace.API.Interfaces;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Profile management endpoints for current authenticated user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProfileAvatarUploadService _profileAvatarUploadService;

    public ProfileController(
        IUserService userService,
        ICurrentUserService currentUserService,
        IProfileAvatarUploadService profileAvatarUploadService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
        _profileAvatarUploadService = profileAvatarUploadService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> GetProfile()
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        var profile = await _userService.GetProfileAsync(currentUserId.Value);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> UpdateProfile([FromBody] ProfileUpdateRequestDTO request)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        var profile = await _userService.UpdateProfileAsync(currentUserId.Value, request);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> UploadAvatar([FromForm] ProfileAvatarUploadRequestDTO request)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        if (!_profileAvatarUploadService.TryValidate(request.Avatar, out var validationError))
        {
            return BadRequest(validationError);
        }

        var avatarUrl = await _profileAvatarUploadService.SaveAsync(request.Avatar!, Request);
        var profile = await _userService.UpdateAvatarAsync(currentUserId.Value, avatarUrl);
        if (profile == null)
        {
            return NotFound();
        }

        return Ok(profile);
    }
}
