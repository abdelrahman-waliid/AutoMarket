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

public class UserSoftDeleteCascadeIntegrationTests
{
    [Fact]
    public async Task DeleteUserAsync_SoftDeletesUserCarsAndMessages_AndHidesThemFromDefaultQueries()
    {
        var context = CreateDbContext();
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        context.Users.AddRange(
            new User
            {
                Id = userId,
                FullName = "Delete Me",
                Email = "delete.me@example.com",
                PasswordHash = "hash",
                FailedLoginAttempts = 0,
                Role = UserRole.User,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            },
            new User
            {
                Id = otherUserId,
                FullName = "Other User",
                Email = "other.user@example.com",
                PasswordHash = "hash",
                FailedLoginAttempts = 0,
                Role = UserRole.User,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            });

        context.Cars.Add(new Car
        {
            Id = carId,
            Title = "Owned Car",
            Brand = "Brand",
            Model = "Model",
            Description = "Car description",
            Price = 12000m,
            Location = "Cairo",
            Status = "Active",
            Views = 10,
            Year = 2020,
            Mileage = 30000,
            FuelType = FuelType.Gasoline,
            TransmissionType = TransmissionType.Automatic,
            OwnerId = userId,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        });

        context.Messages.AddRange(
            new Message
            {
                Id = Guid.NewGuid(),
                SenderId = userId,
                ReceiverId = otherUserId,
                Content = "sent by deleted user",
                CreatedAt = now.AddHours(-2),
                IsRead = false
            },
            new Message
            {
                Id = Guid.NewGuid(),
                SenderId = otherUserId,
                ReceiverId = userId,
                Content = "received by deleted user",
                CreatedAt = now.AddHours(-1),
                IsRead = false
            });

        var refreshTokenId = Guid.NewGuid();
        context.RefreshTokens.Add(new RefreshToken
        {
            Id = refreshTokenId,
            UserId = userId,
            TokenHash = "TOKEN_HASH",
            CreatedAt = now.AddMinutes(-30),
            ExpiresAt = now.AddDays(7),
            IsRevoked = false
        });

        await context.SaveChangesAsync();

        var service = CreateUserService(context);

        var deleted = await service.DeleteUserAsync(userId);

        Assert.True(deleted);

        var user = await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId);
        Assert.True(user.IsDeleted);

        var car = await context.Cars.IgnoreQueryFilters().SingleAsync(c => c.Id == carId);
        Assert.True(car.IsDeleted);

        var relatedMessages = await context.Messages
            .IgnoreQueryFilters()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .ToListAsync();
        Assert.NotEmpty(relatedMessages);
        Assert.All(relatedMessages, message => Assert.True(message.IsDeleted));

        var token = await context.RefreshTokens
            .IgnoreQueryFilters()
            .SingleAsync(rt => rt.Id == refreshTokenId);
        Assert.True(token.IsRevoked);
        Assert.Equal("UserDeleted", token.RevokedReason);

        Assert.Null(await context.Users.FirstOrDefaultAsync(u => u.Id == userId));
        Assert.Null(await context.Cars.FirstOrDefaultAsync(c => c.Id == carId));
        Assert.Empty(await context.Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId).ToListAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"user-soft-delete-{Guid.NewGuid():N}")
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
}
