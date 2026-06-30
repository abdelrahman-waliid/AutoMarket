namespace CarMarketplace.API.Interfaces;

/// <summary>
/// Validates request origin for refresh-cookie endpoints when cross-site cookies are enabled.
/// </summary>
public interface IRefreshRequestCsrfProtectionService
{
    /// <summary>
    /// Returns <c>true</c> when the request is allowed; otherwise <c>false</c>.
    /// Validation is enforced only when refresh cookie SameSite is None.
    /// </summary>
    bool IsRequestAllowed(HttpRequest request);
}
