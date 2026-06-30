using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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

namespace CarMarketplace.Tests.Integration;

public class CarsOwnerDetailsIntegrationTests
{
    private const string JwtIssuer = "cars-owner-details-test-issuer";
    private const string JwtAudience = "cars-owner-details-test-audience";
    private const string JwtSecret = "cars-owner-details-test-secret-key-should-be-at-least-32-chars";

    private static readonly DateTime OwnerCreatedAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OwnerUpdatedAt = new(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task GetCarsPaged_IncludesOwnerDetails()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var carId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await SeedOwnerAndCarAsync(server.Services, ownerId, carId);

        var response = await client.GetAsync("/api/cars?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PaginatedResult<CarDTO>>(JsonOptions);
        Assert.NotNull(paged);

        var car = Assert.Single(paged!.Items);
        Assert.Equal(ownerId, car.OwnerId);
        Assert.Equal("Owner FullName", car.OwnerFullName);
        Assert.Equal("owner@example.com", car.OwnerEmail);
        Assert.Equal("https://example.com/avatar.png", car.OwnerAvatarUrl);
        Assert.Equal(UserRole.User, car.OwnerRole);
        Assert.Equal(OwnerCreatedAt, car.OwnerCreatedAt);
        Assert.Equal(OwnerUpdatedAt, car.OwnerUpdatedAt);
    }

    [Fact]
    public async Task GetMyCarsPaged_IncludesOwnerDetails()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var carId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await SeedOwnerAndCarAsync(server.Services, ownerId, carId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(ownerId, UserRole.User.ToString()));

        var response = await client.GetAsync("/api/my-cars?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PaginatedResult<CarDTO>>(JsonOptions);
        Assert.NotNull(paged);

        var car = Assert.Single(paged!.Items);
        Assert.Equal(ownerId, car.OwnerId);
        Assert.Equal("Owner FullName", car.OwnerFullName);
        Assert.Equal("owner@example.com", car.OwnerEmail);
        Assert.Equal("https://example.com/avatar.png", car.OwnerAvatarUrl);
        Assert.Equal(UserRole.User, car.OwnerRole);
        Assert.Equal(OwnerCreatedAt, car.OwnerCreatedAt);
        Assert.Equal(OwnerUpdatedAt, car.OwnerUpdatedAt);
    }

    [Fact]
    public async Task GetAdminCarsPaged_IncludesOwnerDetails()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var carId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await SeedOwnerAndCarAsync(server.Services, ownerId, carId);

        var adminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(adminId, UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/cars?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PaginatedResult<CarDTO>>(JsonOptions);
        Assert.NotNull(paged);

        var car = Assert.Single(paged!.Items);
        Assert.Equal(ownerId, car.OwnerId);
        Assert.Equal("Owner FullName", car.OwnerFullName);
        Assert.Equal("owner@example.com", car.OwnerEmail);
        Assert.Equal("https://example.com/avatar.png", car.OwnerAvatarUrl);
        Assert.Equal(UserRole.User, car.OwnerRole);
        Assert.Equal(OwnerCreatedAt, car.OwnerCreatedAt);
        Assert.Equal(OwnerUpdatedAt, car.OwnerUpdatedAt);
    }

    [Fact]
    public async Task GetAdminListingsPaged_IncludesOwnerDetails()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var carId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await SeedOwnerAndCarAsync(server.Services, ownerId, carId);

        var adminId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(adminId, UserRole.Admin.ToString()));

        var response = await client.GetAsync("/api/admin/listings?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PaginatedResult<CarDTO>>(JsonOptions);
        Assert.NotNull(paged);

        var car = Assert.Single(paged!.Items);
        Assert.Equal(ownerId, car.OwnerId);
        Assert.Equal("Owner FullName", car.OwnerFullName);
        Assert.Equal("owner@example.com", car.OwnerEmail);
        Assert.Equal("https://example.com/avatar.png", car.OwnerAvatarUrl);
        Assert.Equal(UserRole.User, car.OwnerRole);
        Assert.Equal(OwnerCreatedAt, car.OwnerCreatedAt);
        Assert.Equal(OwnerUpdatedAt, car.OwnerUpdatedAt);
    }

    private static async Task SeedOwnerAndCarAsync(IServiceProvider services, Guid ownerId, Guid carId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new User
        {
            Id = ownerId,
            FullName = "Owner FullName",
            Email = "owner@example.com",
            AvatarUrl = "https://example.com/avatar.png",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = OwnerCreatedAt,
            UpdatedAt = OwnerUpdatedAt
        });

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
        var databaseName = $"cars-owner-details-{Guid.NewGuid():N}";

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

    private static string CreateJwt(Guid userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return tokenHandler.WriteToken(token);
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
