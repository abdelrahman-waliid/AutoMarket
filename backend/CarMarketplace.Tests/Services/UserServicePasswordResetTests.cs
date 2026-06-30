using System.Security.Cryptography;
using System.Text;
using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class UserServicePasswordResetTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenSettings> _refreshTokenSettings = new();
    private readonly UserService _service;

    public UserServicePasswordResetTests()
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
    public async Task ForgotPasswordAsync_WhenUserExists_CreatesHashedToken()
    {
        var user = CreateUser();
        PasswordResetToken? createdToken = null;

        _userRepository.Setup(x => x.GetByEmailTrackedAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordResetTokenRepository.Setup(x => x.InvalidateActiveTokensForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _passwordResetTokenRepository.Setup(x => x.Add(It.IsAny<PasswordResetToken>()))
            .Callback<PasswordResetToken>(token => createdToken = token);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var rawToken = await _service.ForgotPasswordAsync(user.Email);

        Assert.False(string.IsNullOrWhiteSpace(rawToken));
        Assert.NotNull(createdToken);
        Assert.Equal(Hash(rawToken!), createdToken!.TokenHash);
        Assert.NotEqual(rawToken, createdToken.TokenHash);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserMissing_ReturnsNull()
    {
        _userRepository.Setup(x => x.GetByEmailTrackedAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var rawToken = await _service.ForgotPasswordAsync("missing@example.com");

        Assert.Null(rawToken);
        _passwordResetTokenRepository.Verify(x => x.Add(It.IsAny<PasswordResetToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_UpdatesPasswordAndRevokesSessions()
    {
        var rawToken = "raw-reset-token";
        var tokenHash = Hash(rawToken);
        var user = CreateUser();
        var tokenEntity = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _passwordResetTokenRepository.Setup(x => x.GetByTokenHashTrackedAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenEntity);
        _userRepository.Setup(x => x.GetByIdTrackedAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordResetTokenRepository.Setup(x => x.InvalidateActiveTokensForUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _refreshTokenRepository.Setup(x => x.RevokeAllActiveForUserAsync(user.Id, "PasswordReset", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _service.ResetPasswordAsync(rawToken, "NewPassword123!");

        Assert.True(tokenEntity.IsUsed);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123!", user.PasswordHash));
        _refreshTokenRepository.Verify(x => x.RevokeAllActiveForUserAsync(user.Id, "PasswordReset", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ThrowsInvalidOperation()
    {
        _passwordResetTokenRepository.Setup(x => x.GetByTokenHashTrackedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ResetPasswordAsync("invalid-token", "NewPassword123!"));
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Reset User",
            Email = "reset@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static string Hash(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
