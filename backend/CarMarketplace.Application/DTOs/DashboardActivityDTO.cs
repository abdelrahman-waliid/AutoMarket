namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Lightweight activity item for dashboard timeline.
/// </summary>
public class DashboardActivityDTO
{
    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
