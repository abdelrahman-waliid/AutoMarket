namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Generates a cryptographically secure random string for refresh tokens. Implemented in Infrastructure.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a secure random token string.
    /// </summary>
    string Generate();
}
