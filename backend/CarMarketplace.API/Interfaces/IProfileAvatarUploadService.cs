using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.Interfaces;

/// <summary>
/// Handles secure validation and storage of profile avatars.
/// </summary>
public interface IProfileAvatarUploadService
{
    bool TryValidate(IFormFile? avatar, out string? errorMessage);

    Task<string> SaveAsync(IFormFile avatar, HttpRequest request, CancellationToken cancellationToken = default);
}
