using CarMarketplace.Application.Configuration;
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

public class UserRegistrationRestoreIntegrationTests
{
    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesNewUser()
    {
        var context = CreateDbContext();
        var service = CreateUserService(context);

        var result = await service.RegisterAsync(
            "New User",
            "new.user@example.com",
            "Password123!",
            UserRole.User);

        var storedUser = await context.Users.SingleAsync(u => u.Email == "new.user@example.com");
        Assert.Equal(storedUser.Id, result.Id);
        Assert.False(storedUser.IsDeleted);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", storedUser.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingActiveEmail_ThrowsValidationError()
    {
        var context = CreateDbContext();
        var existingUser = CreateUser(
            email: "active.user@example.com",
            fullName: "Active User",
            role: UserRole.User,
            isDeleted: false);

        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var service = CreateUserService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync("Another Name", existingUser.Email, "Password123!", UserRole.User));

        Assert.Equal($"User with email '{existingUser.Email}' already exists.", ex.Message);

        var usersWithEmailCount = await context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.Email == existingUser.Email);
        Assert.Equal(1, usersWithEmailCount);
    }

    [Fact]
    public async Task RegisterAsync_WithSoftDeletedEmail_RestoresExistingUser()
    {
        var context = CreateDbContext();
        var deletedUser = CreateUser(
            email: "deleted.user@example.com",
            fullName: "Old Name",
            role: UserRole.Seller,
            isDeleted: true);
        deletedUser.FailedLoginAttempts = 4;
        deletedUser.LockoutEnd = DateTime.UtcNow.AddMinutes(10);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = deletedUser.Id,
            TokenHash = "SOFT_DELETE_ACTIVE_TOKEN_HASH",
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        context.Users.Add(deletedUser);
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var service = CreateUserService(context);

        var result = await service.RegisterAsync(
            "Restored User",
            deletedUser.Email,
            "NewPassword123!",
            UserRole.User);

        var restoredUser = await context.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Id == deletedUser.Id);

        Assert.Equal(deletedUser.Id, result.Id);
        Assert.False(restoredUser.IsDeleted);
        Assert.Equal("Restored User", restoredUser.FullName);
        Assert.Equal(UserRole.User, restoredUser.Role);
        Assert.Equal(0, restoredUser.FailedLoginAttempts);
        Assert.Null(restoredUser.LockoutEnd);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123!", restoredUser.PasswordHash));

        var revokedToken = await context.RefreshTokens
            .IgnoreQueryFilters()
            .SingleAsync(rt => rt.Id == refreshToken.Id);
        Assert.True(revokedToken.IsRevoked);
        Assert.Equal("ReRegistered", revokedToken.RevokedReason);
        Assert.NotNull(revokedToken.RevokedAt);
    }

    [Fact]
    public async Task RegisterAsync_WithSoftDeletedEmail_DoesNotCreateDuplicateUser()
    {
        var context = CreateDbContext();
        var deletedUser = CreateUser(
            email: "no.duplicate@example.com",
            fullName: "Deleted User",
            role: UserRole.User,
            isDeleted: true);

        context.Users.Add(deletedUser);
        await context.SaveChangesAsync();

        var service = CreateUserService(context);

        var result = await service.RegisterAsync(
            "Restored Without Duplicate",
            deletedUser.Email,
            "AnotherPassword123!",
            UserRole.User);

        var usersWithEmail = await context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email == deletedUser.Email)
            .Select(u => u.Id)
            .ToListAsync();

        Assert.Single(usersWithEmail);
        Assert.Equal(deletedUser.Id, result.Id);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"register-restore-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static UserService CreateUserService(AppDbContext context)
    {
        var tokenService = new Mock<ITokenService>();
        var refreshTokenGenerator = new Mock<IRefreshTokenGenerator>();
        var refreshTokenSettings = new Mock<IRefreshTokenSettings>();
        refreshTokenSettings.SetupGet(x => x.RefreshTokenExpirationDays).Returns(7);

        return new UserService(
            new UserRepository(context),
            new RefreshTokenRepository(context),
            new PasswordResetTokenRepository(context),
            refreshTokenGenerator.Object,
            tokenService.Object,
            new UnitOfWork(context),
            refreshTokenSettings.Object);
    }

    private static User CreateUser(string email, string fullName, UserRole role, bool isDeleted)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            Role = role,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
    }
}
