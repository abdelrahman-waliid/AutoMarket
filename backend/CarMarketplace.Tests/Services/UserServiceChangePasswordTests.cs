using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class UserServiceChangePasswordTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenSettings> _refreshTokenSettings = new();
    private readonly UserService _service;

    public UserServiceChangePasswordTests()
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
    public async Task ChangePasswordAsync_WithValidCurrentPassword_UpdatesHashStampAndRevokesRefreshTokens()
    {
        var user = CreateUser("OldPassword123!");
        var originalStamp = user.SecurityStamp;

        _userRepository.Setup(x => x.GetByIdTrackedAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _refreshTokenRepository.Setup(x => x.RevokeAllActiveForUserAsync(user.Id, "PasswordChange", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var changed = await _service.ChangePasswordAsync(user.Id, "OldPassword123!", "NewPassword123!");

        Assert.True(changed);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123!", user.PasswordHash));
        Assert.NotEqual(originalStamp, user.SecurityStamp);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
        _refreshTokenRepository.Verify(
            x => x.RevokeAllActiveForUserAsync(user.Id, "PasswordChange", It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ReturnsFalseWithoutRevokingTokens()
    {
        var user = CreateUser("OldPassword123!");
        var originalHash = user.PasswordHash;
        var originalStamp = user.SecurityStamp;

        _userRepository.Setup(x => x.GetByIdTrackedAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var changed = await _service.ChangePasswordAsync(user.Id, "WrongPassword123!", "NewPassword123!");

        Assert.False(changed);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.Equal(originalStamp, user.SecurityStamp);
        _refreshTokenRepository.Verify(
            x => x.RevokeAllActiveForUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User CreateUser(string password)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Change Password User",
            Email = "change-password@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12)),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            FailedLoginAttempts = 1,
            LockoutEnd = DateTime.UtcNow.AddMinutes(5),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }
}
