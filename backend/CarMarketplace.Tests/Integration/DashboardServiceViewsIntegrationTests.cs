using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using CarMarketplace.Infrastructure.Data;
using CarMarketplace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarMarketplace.Tests.Integration;

public class DashboardServiceViewsIntegrationTests
{
    [Fact]
    public async Task GetDashboardAsync_WhenUserHasNoCars_ReturnsTotalViewsZero()
    {
        var context = CreateDbContext();
        var userId = Guid.NewGuid();

        var service = new DashboardService(
            new CarRepository(context),
            new MessageRepository(context));

        var dashboard = await service.GetDashboardAsync(userId);

        Assert.Equal(0, dashboard.TotalCarsOwned);
        Assert.Equal(0, dashboard.TotalViewsAcrossListings);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenUserHasMultipleCars_ReturnsCorrectViewsSum()
    {
        var context = CreateDbContext();
        var userId = Guid.NewGuid();

        context.Cars.AddRange(
            new Car
            {
                Id = Guid.NewGuid(),
                Title = "Car A",
                Brand = "Brand",
                Model = "Model",
                Description = "Desc",
                Price = 1000m,
                Location = "Loc",
                Status = "Active",
                Views = 5,
                Year = 2010,
                Mileage = 100000,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Manual,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Car
            {
                Id = Guid.NewGuid(),
                Title = "Car B",
                Brand = "Brand",
                Model = "Model",
                Description = "Desc",
                Price = 2000m,
                Location = "Loc",
                Status = "Active",
                Views = 12,
                Year = 2015,
                Mileage = 80000,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Automatic,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Car
            {
                Id = Guid.NewGuid(),
                Title = "Other Owner",
                Brand = "Brand",
                Model = "Model",
                Description = "Desc",
                Price = 3000m,
                Location = "Loc",
                Status = "Active",
                Views = 999,
                Year = 2020,
                Mileage = 1000,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Automatic,
                OwnerId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            });

        await context.SaveChangesAsync();

        var service = new DashboardService(
            new CarRepository(context),
            new MessageRepository(context));

        var dashboard = await service.GetDashboardAsync(userId);

        Assert.Equal(2, dashboard.TotalCarsOwned);
        Assert.Equal(17, dashboard.TotalViewsAcrossListings);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"dashboard-views-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }
}