using System.Text.Json.Serialization;

namespace CarMarketplace.Application.DTOs;

/// <summary>
/// AI price estimation response for frontend UI usage.
/// </summary>
public class PriceEstimateResponseDTO
{
    public decimal EstimatedPrice { get; set; }

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    public decimal ConfidenceScore { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PriceStatus { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? PercentageDifference { get; set; }

    public List<string> Insights { get; set; } = new();
}
