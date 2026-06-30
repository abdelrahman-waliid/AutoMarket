using CarMarketplace.API.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.Services;

/// <summary>
/// Stores car images under wwwroot/uploads/cars with strict validation.
/// </summary>
public class CarImageUploadService : ICarImageUploadService
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

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string UploadFolder = "uploads/cars";
    private readonly IWebHostEnvironment _environment;

    public CarImageUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool TryValidate(IReadOnlyCollection<IFormFile> images, out string? errorMessage)
    {
        errorMessage = null;

        if (images == null || images.Count == 0)
        {
            errorMessage = "At least one image file is required.";
            return false;
        }

        foreach (var image in images)
        {
            if (image.Length <= 0)
            {
                errorMessage = "Image file is empty.";
                return false;
            }

            if (image.Length > MaxFileSizeBytes)
            {
                errorMessage = $"Image '{image.FileName}' exceeds the 5MB limit.";
                return false;
            }

            var extension = Path.GetExtension(image.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                errorMessage = $"Image '{image.FileName}' has an invalid file type. Only jpg, jpeg, png are allowed.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(image.ContentType) || !AllowedContentTypes.Contains(image.ContentType))
            {
                errorMessage = $"Image '{image.FileName}' has an invalid content type.";
                return false;
            }
        }

        return true;
    }

    public async Task<List<string>> SaveAsync(
        IReadOnlyCollection<IFormFile> images,
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadPath = Path.Combine(webRootPath, "uploads", "cars");
        Directory.CreateDirectory(uploadPath);

        var urls = new List<string>(images.Count);

        foreach (var image in images)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await image.CopyToAsync(stream, cancellationToken);

            urls.Add($"{request.Scheme}://{request.Host}/{UploadFolder}/{fileName}");
        }

        return urls;
    }
}
