using CarMarketplace.Application.DTOs;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Represents the AI abstraction layer of the system.
/// This interface is independent from EF Core and infrastructure concerns.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Estimates a market price range for the provided car details.
    /// </summary>
    /// <param name="request">Price estimate request details.</param>
    /// <returns>Estimated price response.</returns>
    Task<PriceEstimateResponseDTO> EstimatePriceAsync(PriceEstimateRequestDTO request);
}
