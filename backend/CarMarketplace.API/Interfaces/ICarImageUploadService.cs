using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.Interfaces;

/// <summary>
/// Handles secure validation and storage of car images.
/// </summary>
public interface ICarImageUploadService
{
    /// <summary>
    /// Validates uploaded car images.
    /// </summary>
    /// <param name="images">Uploaded files.</param>
    /// <param name="errorMessage">Validation error when invalid.</param>
    /// <returns>True when valid; otherwise false.</returns>
    bool TryValidate(IReadOnlyCollection<IFormFile> images, out string? errorMessage);

    /// <summary>
    /// Saves validated car images and returns public URLs.
    /// </summary>
    /// <param name="images">Validated image files.</param>
    /// <param name="request">Current HTTP request used to generate absolute URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved image URLs.</returns>
    Task<List<string>> SaveAsync(
        IReadOnlyCollection<IFormFile> images,
        HttpRequest request,
        CancellationToken cancellationToken = default);
}
