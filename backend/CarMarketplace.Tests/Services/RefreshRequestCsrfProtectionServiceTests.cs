using CarMarketplace.API.Configuration;
using CarMarketplace.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class RefreshRequestCsrfProtectionServiceTests
{
    [Fact]
    public void IsRequestAllowed_SameSiteNone_AllowedOrigin_Passes()
    {
        var service = CreateService(
            sameSite: SameSiteMode.None,
            allowedOrigins: ["https://app.example.com"]);
        var request = CreateRequestWithOrigin("https://APP.Example.com");

        var allowed = service.IsRequestAllowed(request);

        Assert.True(allowed);
    }

    [Fact]
    public void IsRequestAllowed_SameSiteNone_DisallowedOrigin_Forbidden()
    {
        var service = CreateService(
            sameSite: SameSiteMode.None,
            allowedOrigins: ["https://app.example.com"]);
        var request = CreateRequestWithOrigin("https://evil.example.com");

        var allowed = service.IsRequestAllowed(request);

        Assert.False(allowed);
    }

    [Fact]
    public void IsRequestAllowed_SameSiteNone_MissingOrigin_Forbidden()
    {
        var service = CreateService(
            sameSite: SameSiteMode.None,
            allowedOrigins: ["https://app.example.com"]);
        var request = new DefaultHttpContext().Request;

        var allowed = service.IsRequestAllowed(request);

        Assert.False(allowed);
    }

    [Fact]
    public void IsRequestAllowed_SameSiteLax_MissingOrigin_SkippedAndPasses()
    {
        var service = CreateService(
            sameSite: SameSiteMode.Lax,
            allowedOrigins: ["https://app.example.com"]);
        var request = new DefaultHttpContext().Request;

        var allowed = service.IsRequestAllowed(request);

        Assert.True(allowed);
    }

    private static RefreshRequestCsrfProtectionService CreateService(
        SameSiteMode sameSite,
        string[] allowedOrigins)
    {
        var cookieOptions = Options.Create(new RefreshTokenCookieSettings
        {
            Path = "/api/auth",
            SameSite = sameSite
        });
        var corsOptions = Options.Create(new CorsSettings
        {
            AllowedOrigins = allowedOrigins
        });

        return new RefreshRequestCsrfProtectionService(
            cookieOptions,
            corsOptions,
            NullLogger<RefreshRequestCsrfProtectionService>.Instance);
    }

    private static HttpRequest CreateRequestWithOrigin(string origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Origin = origin;
        return context.Request;
    }
}
