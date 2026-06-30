using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarMarketplace.API.Controllers;
using CarMarketplace.API.Filters;
using CarMarketplace.API.Interfaces;
using CarMarketplace.API.Services;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Application.Validators;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using CarMarketplace.Infrastructure.Data;
using CarMarketplace.Infrastructure.Repositories;
using CarMarketplace.Infrastructure.UnitOfWork;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using System.Text;

namespace CarMarketplace.Tests.Integration;

public class CarsControllerGetByIdIntegrationTests
{
    private const string JwtIssuer = "cars-getbyid-test-issuer";
    private const string JwtAudience = "cars-getbyid-test-audience";
    private const string JwtSecret = "cars-getbyid-test-secret-key-should-be-at-least-32-chars";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task GetCarById_ReturnsOwnerUserInformation()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var carId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var ownerCreatedAt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var ownerUpdatedAt = new DateTime(2021, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        await SeedUserAsync(server.Services, ownerId, ownerCreatedAt, ownerUpdatedAt);
        await SeedCarAsync(server.Services, carId, ownerId);

        var response = await client.GetAsync($"/api/cars/{carId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var car = await response.Content.ReadFromJsonAsync<CarResponseDTO>(JsonOptions);
        Assert.NotNull(car);

        Assert.Equal(ownerId, car!.OwnerId);
        Assert.Equal("Owner FullName", car.OwnerFullName);
        Assert.Equal("https://example.com/avatar.png", car.OwnerAvatarUrl);
        Assert.Equal("owner@example.com", car.OwnerEmail);
        Assert.Equal(UserRole.User, car.OwnerRole);
        Assert.Equal(ownerCreatedAt, car.OwnerCreatedAt);
        Assert.Equal(ownerUpdatedAt, car.OwnerUpdatedAt);
        Assert.Equal("BMW 320i 2010 Excellent Condition", car.Title);
        Assert.Single(car.ImageUrls);
    }

    private static async Task SeedUserAsync(IServiceProvider services, Guid userId, DateTime createdAt, DateTime updatedAt)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new User
        {
            Id = userId,
            FullName = "Owner FullName",
            Email = "owner@example.com",
            AvatarUrl = "https://example.com/avatar.png",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCarAsync(IServiceProvider services, Guid carId, Guid ownerId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Cars.Add(new Car
        {
            Id = carId,
            Title = "BMW 320i 2010 Excellent Condition",
            Brand = "BMW",
            Model = "320i",
            Description = "Used BMW in excellent condition",
            Price = 13500,
            Location = "Cairo",
            Status = "Active",
            Views = 0,
            Year = 2010,
            Mileage = 145000,
            FuelType = FuelType.Gasoline,
            TransmissionType = TransmissionType.Manual,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.CarImages.Add(new CarImage
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CarId = carId,
            ImageUrl = "https://example.com/car1.jpg",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static TestServer CreateServer()
    {
        var databaseName = $"cars-getbyid-{Guid.NewGuid():N}";

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddControllers(options =>
                    {
                        options.Filters.Add<FluentValidationFilter>();
                    })
                    .AddApplicationPart(typeof(CarsController).Assembly)
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

                services.AddValidatorsFromAssemblyContaining<CreateCarRequestDTOValidator>();
                services.AddScoped<FluentValidationFilter>();
                services.AddHttpContextAccessor();

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                services.AddScoped<ICarRepository, CarRepository>();
                services.AddScoped<IUserRepository, UserRepository>();
                services.AddScoped<IUnitOfWork, UnitOfWork>();
                services.AddScoped<IAIService, AIService>();
                services.AddScoped<ICurrentUserService, CurrentUserService>();
                services.AddScoped<ICarService, CarService>();
                services.AddSingleton<ICarImageUploadService, StubCarImageUploadService>();

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = JwtIssuer,
                            ValidAudience = JwtAudience,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
                            ClockSkew = TimeSpan.Zero
                        };
                    });

                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            });

        return new TestServer(webHostBuilder);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class StubCarImageUploadService : ICarImageUploadService
    {
        public bool TryValidate(IReadOnlyCollection<IFormFile> images, out string? errorMessage)
        {
            errorMessage = null;
            return true;
        }

        public Task<List<string>> SaveAsync(
            IReadOnlyCollection<IFormFile> images,
            HttpRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<string>());
        }
    }
}
