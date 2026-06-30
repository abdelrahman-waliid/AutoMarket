using Microsoft.AspNetCore.Http;

namespace CarMarketplace.API.Configuration;

/// <summary>
/// Configures refresh-token cookie behavior.
/// </summary>
public class RefreshTokenCookieSettings
{
    public const string SectionName = "RefreshTokenCookie";

    /// <summary>
    /// Cookie path scope. Keep this as narrow as possible.
    /// </summary>
    public string Path { get; set; } = "/api/auth";

    /// <summary>
    /// SameSite mode: Strict, Lax, None.
    /// </summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
}
