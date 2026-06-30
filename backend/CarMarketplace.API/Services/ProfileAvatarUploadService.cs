using CarMarketplace.API.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.Services;

/// <summary>
/// Stores profile avatars under wwwroot/uploads/avatars with strict validation.
/// </summary>
public class ProfileAvatarUploadService : IProfileAvatarUploadService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png"
    };

    private const long MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB
    private const string UploadFolder = "uploads/avatars";
    private readonly IWebHostEnvironment _environment;

    public ProfileAvatarUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool TryValidate(IFormFile? avatar, out string? errorMessage)
    {
        errorMessage = null;

        if (avatar == null)
        {
            errorMessage = "Avatar file is required.";
            return false;
        }

        if (avatar.Length <= 0)
        {
            errorMessage = "Avatar file is empty.";
            return false;
        }

        if (avatar.Length > MaxFileSizeBytes)
        {
            errorMessage = "Avatar exceeds the 1MB limit.";
            return false;
        }

        var extension = Path.GetExtension(avatar.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            errorMessage = "Invalid avatar file type. Only jpg, jpeg, png are allowed.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(avatar.ContentType) || !AllowedContentTypes.Contains(avatar.ContentType))
        {
            errorMessage = "Invalid avatar content type.";
            return false;
        }

        return true;
    }

    public async Task<string> SaveAsync(IFormFile avatar, HttpRequest request, CancellationToken cancellationToken = default)
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadPath = Path.Combine(webRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadPath);

        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.CreateNew);
        await avatar.CopyToAsync(stream, cancellationToken);

        return $"{request.Scheme}://{request.Host}/{UploadFolder}/{fileName}";
    }
}
