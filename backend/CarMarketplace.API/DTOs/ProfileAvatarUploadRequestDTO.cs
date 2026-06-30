using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.DTOs;

/// <summary>
/// Multipart payload for profile avatar upload.
/// </summary>
public class ProfileAvatarUploadRequestDTO
{
    public IFormFile? Avatar { get; set; }
}
