using CarMarketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.DTOs;

/// <summary>
/// Request model for creating a car with uploaded images via multipart/form-data.
/// </summary>
public class CreateCarWithImagesRequestDTO
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

    public List<IFormFile> Images { get; set; } = new();
}
