namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for car image information.
/// Used for transferring car image data between frontend and backend.
/// </summary>
public class CarImageDTO
{
    /// <summary>
    /// Gets or sets the unique identifier for the car image.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the URL of the car image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
}
