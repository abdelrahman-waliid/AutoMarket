using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using CarMarketplace.Infrastructure.Data;
using CarMarketplace.Infrastructure.Repositories;
using CarMarketplace.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Integration;

public class RefreshTokenFlowIntegrationTests
{
    [Fact]
    public async Task RefreshAccessTokenAsync_RotatesToken_ChangesAccessToken_AndRevokesOldToken()
    {
        var context = CreateDbContext();
        var user = await SeedUserAsync(context);
        var service = CreateUserService(
            context,
            accessTokens: ["access-token-1", "access-token-2"],
            refreshTokens: ["refresh-token-1", "refresh-token-2"]);

        var loginSession = await service.AuthenticateAsync(user.Email, "password123");
        var refreshedSession = await service.RefreshAccessTokenAsync(loginSession!.RefreshToken);

        Assert.NotNull(refreshedSession);
        Assert.NotEqual(loginSession.Response.Token, refreshedSession!.Response.Token);
        Assert.NotEqual(loginSession.RefreshToken, refreshedSession.RefreshToken);

        var oldTokenHash = Hash(loginSession.RefreshToken);
        var oldToken = await context.RefreshTokens.SingleAsync(rt => rt.TokenHash == oldTokenHash);
        Assert.True(oldToken.IsRevoked);
        Assert.Equal("Rotated", oldToken.RevokedReason);

        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();

        Assert.Single(activeTokens);
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_ReusedToken_RevokesAllActiveTokens()
    {
        var context = CreateDbContext();
        var user = await SeedUserAsync(context);
        var service = CreateUserService(
            context,
            accessTokens: ["access-token-1", "access-token-2"],
            refreshTokens: ["refresh-token-1", "refresh-token-2"]);

        var loginSession = await service.AuthenticateAsync(user.Email, "password123");
        await service.RefreshAccessTokenAsync(loginSession!.RefreshToken);

        await Assert.ThrowsAsync<RefreshTokenReuseDetectedException>(() =>
            service.RefreshAccessTokenAsync(loginSession.RefreshToken));

        var activeTokensRemain = await context.RefreshTokens
            .AnyAsync(rt => rt.UserId == user.Id && !rt.IsRevoked);

        Assert.False(activeTokensRemain);
    }

    [Fact]
    public async Task ChangePasswordAsync_RevokesExistingRefreshToken()
    {
        var context = CreateDbContext();
        var user = await SeedUserAsync(context, "OldPassword123!");
        var service = CreateUserService(
            context,
            accessTokens: ["access-token-1"],
            refreshTokens: ["refresh-token-1"]);

        var loginSession = await service.AuthenticateAsync(user.Email, "OldPassword123!");
        Assert.NotNull(loginSession);

        var changed = await service.ChangePasswordAsync(user.Id, "OldPassword123!", "NewPassword123!");
        var refreshedSession = await service.RefreshAccessTokenAsync(loginSession!.RefreshToken);

        Assert.True(changed);
        Assert.Null(refreshedSession);

        var activeTokensRemain = await context.RefreshTokens
            .AnyAsync(rt => rt.UserId == user.Id && !rt.IsRevoked);
        Assert.False(activeTokensRemain);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"refresh-flow-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<User> SeedUserAsync(AppDbContext context, string password = "password123")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Integration User",
            Email = "integration@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12)),
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

    private static string Hash(string rawValue)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawValue);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
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
