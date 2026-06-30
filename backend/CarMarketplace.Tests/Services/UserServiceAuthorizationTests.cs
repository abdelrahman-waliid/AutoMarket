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

public class UserServiceAuthorizationTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRefreshTokenSettings> _refreshTokenSettings = new();
    private readonly UserService _service;

    public UserServiceAuthorizationTests()
    {
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
    public async Task UpdateUserAsync_WhenCurrentUserIdIsEmpty_ThrowsUnauthorized()
    {
        var dto = new UserDTO
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            Role = UserRole.User
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateUserAsync(dto, UserRole.User, Guid.Empty));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNonAdminUpdatesAnotherUser_ThrowsForbidden()
    {
        var dto = new UserDTO
        {
            Id = Guid.NewGuid(),
            FullName = "User",
            Email = "user@test.com",
            Role = UserRole.User
        };
        var currentUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.UpdateUserAsync(dto, UserRole.User, currentUserId));

        _userRepository.Verify(x => x.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNonAdminUpdatesOwnProfile_DoesNotChangeRole()
    {
        var currentUserId = Guid.NewGuid();
        var user = new User
        {
            Id = currentUserId,
            FullName = "Old Name",
            Email = "old@test.com",
            PasswordHash = "hash",
            Role = UserRole.Seller,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var dto = new UserDTO
        {
            Id = currentUserId,
            FullName = "New Name",
            Email = "new@test.com",
            Role = UserRole.Admin // non-admin caller must not be able to elevate
        };

        _userRepository.Setup(x => x.GetByIdTrackedAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(x => x.ExistsByEmailAsync(dto.Email, dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.UpdateUserAsync(dto, UserRole.User, currentUserId);

        Assert.NotNull(result);
        Assert.Equal(dto.FullName, user.FullName);
        Assert.Equal(dto.Email, user.Email);
        Assert.Equal(UserRole.Seller, user.Role);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenAdminUpdatesAnotherUser_CanChangeRole()
    {
        var currentAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = new User
        {
            Id = targetUserId,
            FullName = "User",
            Email = "user@test.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var dto = new UserDTO
        {
            Id = targetUserId,
            FullName = "Updated User",
            Email = "updated@test.com",
            Role = UserRole.Seller
        };

        _userRepository.Setup(x => x.GetByIdTrackedAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository.Setup(x => x.ExistsByEmailAsync(dto.Email, dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.UpdateUserAsync(dto, UserRole.Admin, currentAdminId);

        Assert.NotNull(result);
        Assert.Equal(UserRole.Seller, user.Role);
        Assert.Equal(dto.FullName, result!.FullName);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenTargetUserNotFound_ReturnsNull()
    {
        var currentUserId = Guid.NewGuid();
        var dto = new UserDTO
        {
            Id = currentUserId,
            FullName = "User",
            Email = "user@test.com",
            Role = UserRole.User
        };

        _userRepository.Setup(x => x.GetByIdTrackedAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.UpdateUserAsync(dto, UserRole.User, currentUserId);

        Assert.Null(result);
    }
}
