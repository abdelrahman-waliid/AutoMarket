using System.Security.Cryptography;
using System.Text;
using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class UserServiceRefreshTokenSecurityTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenSettings> _refreshTokenSettings = new();
    private readonly UserService _service;

    public UserServiceRefreshTokenSecurityTests()
    {
        _refreshTokenSettings.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);

        _service = new UserService(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordResetTokenRepository.Object,
            _refreshTokenGenerator.Object,
            _tokenService.Object,
            _unitOfWork.Object,
            _refreshTokenSettings.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_StoresHashedRefreshToken()
    {
        var rawRefreshToken = "refresh-token-raw-value";
        var user = CreateUser();
        RefreshToken? storedEntity = null;

        _userRepository.Setup(x => x.GetByEmailTrackedAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenService.Setup(x => x.GenerateToken(It.IsAny<UserDTO>())).Returns("access-token");
        _refreshTokenGenerator.Setup(x => x.Generate()).Returns(rawRefreshToken);
        _refreshTokenRepository.Setup(x => x.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(entity => storedEntity = entity);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.AuthenticateAsync(user.Email, "password123");

        Assert.NotNull(result);
        Assert.Equal(rawRefreshToken, result!.RefreshToken);
        Assert.NotNull(storedEntity);
        Assert.Equal(Hash(rawRefreshToken), storedEntity!.TokenHash);
        Assert.NotEqual(rawRefreshToken, storedEntity.TokenHash);
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_ValidToken_RotatesRefreshToken()
    {
        var oldRawToken = "old-raw-token";
        var newRawToken = "new-raw-token";
        var user = CreateUser();
        var oldHash = Hash(oldRawToken);
        RefreshToken? newEntity = null;

        var existingEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = oldHash,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiryDate = DateTime.UtcNow.AddDays(5),
            IsRevoked = false
        };

        _refreshTokenRepository.Setup(x => x.GetByTokenHashTrackedAsync(oldHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _userRepository.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenService.Setup(x => x.GenerateToken(It.IsAny<UserDTO>())).Returns("new-access-token");
        _refreshTokenGenerator.Setup(x => x.Generate()).Returns(newRawToken);
        _refreshTokenRepository.Setup(x => x.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(entity => newEntity = entity);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.RefreshAccessTokenAsync(oldRawToken);

        Assert.NotNull(result);
        Assert.Equal("new-access-token", result!.Response.Token);
        Assert.Equal(newRawToken, result.RefreshToken);

        Assert.True(existingEntity.IsRevoked);
        Assert.Equal("Rotated", existingEntity.RevokedReason);
        Assert.Equal(Hash(newRawToken), existingEntity.ReplacedByTokenHash);
        Assert.NotNull(existingEntity.RevokedAt);

        Assert.NotNull(newEntity);
        Assert.Equal(user.Id, newEntity!.UserId);
        Assert.Equal(Hash(newRawToken), newEntity.TokenHash);
        Assert.False(newEntity.IsRevoked);

        _refreshTokenRepository.Verify(x => x.RevokeAllActiveForUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_ReusedRotatedToken_DeniesAndRevokesActiveTokens()
    {
        var reusedRawToken = "reused-raw-token";
        var hash = Hash(reusedRawToken);
        var userId = Guid.NewGuid();
        var revokedEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiryDate = DateTime.UtcNow.AddDays(5),
            IsRevoked = true,
            RevokedReason = "Rotated",
            ReplacedByTokenHash = Hash("new-token")
        };

        _refreshTokenRepository.Setup(x => x.GetByTokenHashTrackedAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedEntity);
        _refreshTokenRepository.Setup(x => x.RevokeAllActiveForUserAsync(userId, "ReuseDetected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await Assert.ThrowsAsync<RefreshTokenReuseDetectedException>(() =>
            _service.RefreshAccessTokenAsync(reusedRawToken));
        _refreshTokenRepository.Verify(x => x.RevokeAllActiveForUserAsync(userId, "ReuseDetected", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_UsesHashedTokenLookup()
    {
        var rawToken = "logout-raw-token";
        var hash = Hash(rawToken);

        _refreshTokenRepository.Setup(x => x.RevokeByTokenHashAsync(hash, "Logout", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var revoked = await _service.RevokeRefreshTokenAsync(rawToken);

        Assert.True(revoked);
        _refreshTokenRepository.Verify(x => x.RevokeByTokenHashAsync(hash, "Logout", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", BCrypt.Net.BCrypt.GenerateSalt(12)),
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static string Hash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
