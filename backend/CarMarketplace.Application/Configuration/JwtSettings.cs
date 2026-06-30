namespace CarMarketplace.Application.Configuration;

/// <summary>
/// JWT configuration bound from appsettings.
/// In production, set SecretKey via environment variable (e.g. JwtSettings__SecretKey) or user secrets.
/// </summary>
public class JwtSettings : IRefreshTokenSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Symmetric security key used to sign tokens. Must be at least 32 characters in production.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (e.g. your API name).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (e.g. your client app name).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token validity in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
