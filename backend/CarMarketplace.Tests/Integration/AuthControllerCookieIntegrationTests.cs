using CarMarketplace.API.Configuration;
using CarMarketplace.API.Controllers;
using CarMarketplace.API.Interfaces;
using CarMarketplace.API.Services;
using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using CarMarketplace.Infrastructure.Data;
using CarMarketplace.Infrastructure.Repositories;
using CarMarketplace.Infrastructure.UnitOfWork;
using CarMarketplace.Tests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Integration;

public class AuthControllerCookieIntegrationTests
{
    [Fact]
    public async Task Refresh_RotatesToken_AndSetsSecureCookieFlags()
    {
        var context = CreateDbContext();
        var user = await SeedUserAsync(context);
        var userService = CreateUserService(
            context,
            accessTokens: ["access-token-1", "access-token-2"],
            refreshTokens: ["refresh-token-1", "refresh-token-2"]);

        var loginSession = await userService.AuthenticateAsync(user.Email, "password123");
        Assert.NotNull(loginSession);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Headers.Origin = "https://app.example.com";
        httpContext.Request.Headers.Cookie = $"refreshToken={loginSession!.RefreshToken}";

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var cookieSettings = Options.Create(new RefreshTokenCookieSettings
        {
            Path = "/api/auth",
            SameSite = SameSiteMode.None
        });

        var cookieService = new RefreshTokenCookieService(
            Options.Create(new JwtSettings
            {
                RefreshTokenExpirationDays = 7,
                SecretKey = new string('x', 32),
                Issuer = "issuer",
                Audience = "audience"
            }),
            cookieSettings,
            httpContextAccessor,
            new TestWebHostEnvironment { EnvironmentName = Environments.Production },
            NullLogger<RefreshTokenCookieService>.Instance);

        var csrfService = new RefreshRequestCsrfProtectionService(
            cookieSettings,
            Options.Create(new CorsSettings { AllowedOrigins = ["https://app.example.com"] }),
            NullLogger<RefreshRequestCsrfProtectionService>.Instance);

        var controller = new AuthController(
            userService,
            cookieService,
            csrfService,
            Mock.Of<IPasswordResetEmailService>())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await controller.Refresh();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponseDTO>(ok.Value);
        Assert.Equal("access-token-2", payload.Token);

        var setCookieHeader = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("refreshToken=refresh-token-2", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token-1", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpOnly", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=None", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/api/auth", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"auth-cookie-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<User> SeedUserAsync(AppDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Cookie Test User",
            Email = "cookie-test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", BCrypt.Net.BCrypt.GenerateSalt(12)),
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static UserService CreateUserService(
        AppDbContext context,
        IReadOnlyList<string> accessTokens,
        IReadOnlyList<string> refreshTokens)
    {
        var tokenService = new Mock<ITokenService>();
        var tokenSetup = tokenService.SetupSequence(ts => ts.GenerateToken(It.IsAny<UserDTO>()));
        foreach (var accessToken in accessTokens)
        {
            tokenSetup = tokenSetup.Returns(accessToken);
        }

        var refreshTokenSettings = new Mock<IRefreshTokenSettings>();
        refreshTokenSettings.SetupGet(s => s.RefreshTokenExpirationDays).Returns(7);

        return new UserService(
            new UserRepository(context),
            new RefreshTokenRepository(context),
            new PasswordResetTokenRepository(context),
            new QueueRefreshTokenGenerator(refreshTokens),
            tokenService.Object,
            new UnitOfWork(context),
            refreshTokenSettings.Object);
    }

    private sealed class QueueRefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly Queue<string> _tokens;

        public QueueRefreshTokenGenerator(IEnumerable<string> tokens)
        {
            _tokens = new Queue<string>(tokens);
        }

        public string Generate()
        {
            if (_tokens.Count == 0)
            {
                throw new InvalidOperationException("No refresh tokens left in test generator queue.");
            }

            return _tokens.Dequeue();
        }
    }
}
