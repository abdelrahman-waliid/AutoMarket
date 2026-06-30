using CarMarketplace.Application.DTOs;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Provides user dashboard aggregates.
/// </summary>
public interface IDashboardService
{
    Task<DashboardDTO> GetDashboardAsync(Guid currentUserId);
}
