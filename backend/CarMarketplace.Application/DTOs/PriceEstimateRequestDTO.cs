namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Input payload for AI price estimation.
/// </summary>
public class PriceEstimateRequestDTO
{
    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Mileage { get; set; }

    public string Condition { get; set; } = string.Empty;

    public string Transmission { get; set; } = string.Empty;

    public string FuelType { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Optional listed/user-entered price used for status classification.
    /// </summary>
    public decimal? UserPrice { get; set; }
}
