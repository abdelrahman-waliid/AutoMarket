using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Client payload for creating a new car listing.
/// Server-controlled fields are generated in the backend.
/// </summary>
public class CreateCarRequestDTO
{
    public string Title { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Location { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Mileage { get; set; }

    public FuelType FuelType { get; set; }

    public TransmissionType TransmissionType { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}
