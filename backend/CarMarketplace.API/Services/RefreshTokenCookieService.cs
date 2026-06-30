using CarMarketplace.API.Interfaces;
using CarMarketplace.API.Configuration;
using CarMarketplace.Application.Configuration;
using Microsoft.Extensions.Options;

namespace CarMarketplace.API.Services;

/// <summary>
/// Issues refresh-token cookies with secure defaults for browser clients.
/// </summary>
public sealed class RefreshTokenCookieService : IRefreshTokenCookieService
{
    public const string RefreshTokenCookieName = "refreshToken";

    private readonly JwtSettings _jwtSettings;
    private readonly RefreshTokenCookieSettings _cookieSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RefreshTokenCookieService> _logger;

    public RefreshTokenCookieService(
        IOptions<JwtSettings> jwtSettings,
        IOptions<RefreshTokenCookieSettings> cookieSettings,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment,
        ILogger<RefreshTokenCookieService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _cookieSettings = cookieSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _logger = logger;
    }

    public void SetCookie(HttpResponse response, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        response.Cookies.Append(RefreshTokenCookieName, refreshToken, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)));
    }

    public void ClearCookie(HttpResponse response)
    {
        response.Cookies.Delete(RefreshTokenCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch));
    }

    public bool TryGetFromRequest(HttpRequest request, out string refreshToken)
    {
        if (request.Cookies.TryGetValue(RefreshTokenCookieName, out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            refreshToken = token;
            return true;
        }

        refreshToken = string.Empty;
        return false;
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookie(),
            SameSite = _cookieSettings.SameSite,
            Expires = expires,
            Path = _cookieSettings.Path
        };
    }

    private bool ShouldUseSecureCookie()
    {
        var request = _httpContextAccessor.HttpContext?.Request;

        if (!_environment.IsDevelopment())
        {
            if (request != null && !request.IsHttps)
            {
                _logger.LogWarning(
                    "Issuing refresh-token cookie while request is not HTTPS in non-development environment.");
            }

            return true;
        }

        if (_cookieSettings.SameSite == SameSiteMode.None)
        {
            // Browsers require Secure when SameSite=None.
            return true;
        }

        if (request != null)
        {
            return request.IsHttps;
        }

        return false;
    }
}
