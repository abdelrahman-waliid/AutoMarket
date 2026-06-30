namespace CarMarketplace.API.Interfaces;

/// <summary>
/// Handles issuing and clearing refresh token cookies.
/// </summary>
public interface IRefreshTokenCookieService
{
    /// <summary>
    /// Writes the refresh token cookie with secure flags.
    /// </summary>
    void SetCookie(HttpResponse response, string refreshToken);

    /// <summary>
    /// Deletes the refresh token cookie.
    /// </summary>
    void ClearCookie(HttpResponse response);

    /// <summary>
    /// Reads refresh token from request cookie.
    /// </summary>
    bool TryGetFromRequest(HttpRequest request, out string refreshToken);
}
