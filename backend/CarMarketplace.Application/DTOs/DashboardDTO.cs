namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Dashboard aggregate response for current user.
/// </summary>
public class DashboardDTO
{
    public int TotalCarsOwned { get; set; }

    public int UnreadMessagesCount { get; set; }

    public int TotalViewsAcrossListings { get; set; }

    public List<DashboardActivityDTO> RecentActivity { get; set; } = new();
}
