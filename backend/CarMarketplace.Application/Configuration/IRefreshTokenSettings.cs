namespace CarMarketplace.Application.Configuration;

/// <summary>
/// Provides refresh token configuration. Implemented via JwtSettings in the API host.
/// </summary>
public interface IRefreshTokenSettings
{
    /// <summary>
    /// Refresh token validity in days.
    /// </summary>
    int RefreshTokenExpirationDays { get; }
}
