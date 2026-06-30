using CarMarketplace.API.Configuration;
using CarMarketplace.API.Interfaces;
using Microsoft.Extensions.Options;

namespace CarMarketplace.API.Services;

/// <summary>
/// Origin-based CSRF protection for refresh-cookie endpoints.
/// Applies only when SameSite=None.
/// </summary>
public class RefreshRequestCsrfProtectionService : IRefreshRequestCsrfProtectionService
{
    private readonly RefreshTokenCookieSettings _cookieSettings;
    private readonly HashSet<string> _allowedOrigins;
    private readonly ILogger<RefreshRequestCsrfProtectionService> _logger;

    public RefreshRequestCsrfProtectionService(
        IOptions<RefreshTokenCookieSettings> cookieSettings,
        IOptions<CorsSettings> corsSettings,
        ILogger<RefreshRequestCsrfProtectionService> logger)
    {
        _cookieSettings = cookieSettings.Value;
        _logger = logger;
        _allowedOrigins = corsSettings.Value.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(NormalizeOrigin)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool IsRequestAllowed(HttpRequest request)
    {
        if (_cookieSettings.SameSite != SameSiteMode.None)
        {
            return true;
        }

        if (!request.Headers.TryGetValue("Origin", out var originValues))
        {
            _logger.LogWarning("Rejected refresh-cookie request: Origin header is missing while SameSite=None.");
            return false;
        }

        var origin = NormalizeOrigin(originValues.ToString());
        if (string.IsNullOrWhiteSpace(origin))
        {
            _logger.LogWarning("Rejected refresh-cookie request: Origin header is empty while SameSite=None.");
            return false;
        }

        if (_allowedOrigins.Contains(origin))
        {
            return true;
        }

        _logger.LogWarning(
            "Rejected refresh-cookie request from untrusted origin '{Origin}' while SameSite=None.",
            origin);

        return false;
    }

    private static string NormalizeOrigin(string origin)
    {
        return origin.Trim().TrimEnd('/');
    }
}
