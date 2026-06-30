using CarMarketplace.API.Configuration;
using CarMarketplace.API.Services;
using CarMarketplace.Application.Configuration;
using CarMarketplace.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class RefreshTokenCookieServiceTests
{
    [Fact]
    public void SetCookie_ProductionHttpRequest_UsesSecureFlagsAndLogsWarning()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var logger = new RecordingLogger<RefreshTokenCookieService>();

        var service = new RefreshTokenCookieService(
            Options.Create(new JwtSettings { RefreshTokenExpirationDays = 7 }),
            Options.Create(new RefreshTokenCookieSettings { Path = "/api/auth", SameSite = SameSiteMode.Lax }),
            httpContextAccessor,
            new TestWebHostEnvironment { EnvironmentName = Environments.Production },
            logger);

        service.SetCookie(context.Response, "refresh-token-value");

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("HttpOnly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=Lax", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/api/auth", header, StringComparison.OrdinalIgnoreCase);
        Assert.Single(logger.WarningMessages);
    }

    [Fact]
    public void SetCookie_SameSiteNone_AlwaysUsesSecure()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };

        var service = new RefreshTokenCookieService(
            Options.Create(new JwtSettings { RefreshTokenExpirationDays = 7 }),
            Options.Create(new RefreshTokenCookieSettings { Path = "/api/auth", SameSite = SameSiteMode.None }),
            httpContextAccessor,
            new TestWebHostEnvironment { EnvironmentName = Environments.Development },
            new RecordingLogger<RefreshTokenCookieService>());

        service.SetCookie(context.Response, "refresh-token-value");

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("Secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=None", header, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> WarningMessages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningMessages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
