using CarMarketplace.Application.DTOs;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using CarMarketplace.Infrastructure.Data;
using CarMarketplace.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarMarketplace.Tests.Integration;

public class CarRepositoryPaginationIntegrationTests
{
    [Fact]
    public async Task GetPagedWithImagesAsync_AppliesDatabasePaginationAndReturnsTotalCount()
    {
        await using var context = CreateDbContext();
        var ownerId = await SeedOwnerAsync(context);
        await SeedCarsAsync(context, ownerId, 25);

        var repository = new CarRepository(context);
        var query = new CarQueryParametersDTO
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = CarSortBy.CreatedAt,
            SortOrder = "desc"
        };

        var (items, totalCount) = await repository.GetPagedWithImagesAsync(query);

        Assert.Equal(25, totalCount);
        Assert.Equal(10, items.Count);
        Assert.All(items, car => Assert.NotEqual(Guid.Empty, car.Id));
    }

    [Fact]
    public async Task GetPagedWithImagesAsync_BrandAndSearchFilters_WorkTogether()
    {
        await using var context = CreateDbContext();
        var ownerId = await SeedOwnerAsync(context);

        context.Cars.AddRange(
            CreateCar(ownerId, "Toyota", "Corolla", DateTime.UtcNow.AddMinutes(-3)),
            CreateCar(ownerId, "Toyota", "Camry", DateTime.UtcNow.AddMinutes(-2)),
            CreateCar(ownerId, "Honda", "Civic", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();

        var repository = new CarRepository(context);
        var query = new CarQueryParametersDTO
        {
            PageNumber = 1,
            PageSize = 10,
            Brand = "Toyota",
            Search = "Camry",
            SortBy = CarSortBy.CreatedAt,
            SortOrder = "desc"
        };

        var (items, totalCount) = await repository.GetPagedWithImagesAsync(query);

        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("Toyota", items[0].Brand);
        Assert.Equal("Camry", items[0].Model);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"car-repo-paging-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Guid> SeedOwnerAsync(AppDbContext context)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Owner",
            Email = $"owner-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        await context.SaveChangesAsync();
        return owner.Id;
    }

    private static async Task SeedCarsAsync(AppDbContext context, Guid ownerId, int count)
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < count; i++)
        {
            context.Cars.Add(CreateCar(ownerId, "Brand", $"Model-{i}", now.AddMinutes(-i)));
        }

        await context.SaveChangesAsync();
    }

    private static Car CreateCar(Guid ownerId, string brand, string model, DateTime createdAt)
    {
        return new Car
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = $"{brand} {model}",
            Brand = brand,
            Model = model,
            Description = "Test car",
            Price = 10000,
            Location = "Cairo",
            Status = "Active",
            Views = 0,
            Year = 2022,
            Mileage = 10000,
            FuelType = FuelType.Gasoline,
            TransmissionType = TransmissionType.Automatic,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
