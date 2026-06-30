using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for car information.
/// Used for transferring car data between frontend and backend.
/// </summary>
public class CarDTO
{
    /// <summary>
    /// Gets or sets the unique identifier for the car.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the car listing.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the car brand.
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the car model.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the car.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price of the car.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets listing location.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets listing status.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets listing view count.
    /// </summary>
    public int Views { get; set; }

    /// <summary>
    /// Gets or sets the manufacturing year of the car.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the mileage of the car in kilometers or miles.
    /// </summary>
    public int Mileage { get; set; }

    /// <summary>
    /// Gets or sets the fuel type of the car.
    /// </summary>
    public FuelType FuelType { get; set; }

    /// <summary>
    /// Gets or sets the transmission type of the car.
    /// </summary>
    public TransmissionType TransmissionType { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the car owner.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the owner's full name (for UI display).
    /// </summary>
    public string? OwnerFullName { get; set; }

    /// <summary>
    /// Gets or sets the owner's email (for UI display).
    /// </summary>
    public string? OwnerEmail { get; set; }

    /// <summary>
    /// Gets or sets the owner's avatar URL (for UI display).
    /// </summary>
    public string? OwnerAvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the owner's role (for UI display).
    /// </summary>
    public UserRole? OwnerRole { get; set; }

    /// <summary>
    /// Gets or sets the owner's created timestamp.
    /// </summary>
    public DateTime? OwnerCreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the owner's updated timestamp.
    /// </summary>
    public DateTime? OwnerUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets updated timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the list of image URLs associated with the car.
    /// </summary>
    public List<string> ImageUrls { get; set; } = new List<string>();
}
