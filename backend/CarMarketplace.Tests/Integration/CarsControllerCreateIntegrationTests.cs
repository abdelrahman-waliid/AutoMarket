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

public class CarsControllerCreateIntegrationTests
{
    private const string JwtIssuer = "cars-test-issuer";
    private const string JwtAudience = "cars-test-audience";
    private const string JwtSecret = "cars-test-secret-key-should-be-at-least-32-chars";

    private static readonly DateTime SeedCreatedAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SeedUpdatedAt = new(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task CreateCar_AuthenticatedUser_CreatesListing()
    {
        using var server = CreateServer();
        await SeedUserAsync(server.Services, TestUsers.UserAId, "usera@example.com");
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(TestUsers.UserAId));

        var request = CreateValidRequest();
        var response = await client.PostAsJsonAsync("/api/cars", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var car = await response.Content.ReadFromJsonAsync<CarDTO>(JsonOptions);
        Assert.NotNull(car);
        Assert.NotEqual(Guid.Empty, car!.Id);
        Assert.Equal(request.Title, car.Title);
        Assert.Equal(request.Brand, car.Brand);
        Assert.Equal(request.Model, car.Model);

        Assert.Equal(TestUsers.UserAId, car.OwnerId);
        Assert.Equal("Test User", car.OwnerFullName);
        Assert.Equal("usera@example.com", car.OwnerEmail);
        Assert.Equal("https://example.com/avatar.png", car.OwnerAvatarUrl);
        Assert.Equal(UserRole.User, car.OwnerRole);
        Assert.Equal(SeedCreatedAt, car.OwnerCreatedAt);
        Assert.Equal(SeedUpdatedAt, car.OwnerUpdatedAt);
    }

    [Fact]
    public async Task CreateCar_OwnerId_IsTakenFromJwt()
    {
        using var server = CreateServer();
        await SeedUserAsync(server.Services, TestUsers.UserBId, "userb@example.com");
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(TestUsers.UserBId));

        var response = await client.PostAsJsonAsync("/api/cars", CreateValidRequest(), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var car = await response.Content.ReadFromJsonAsync<CarDTO>(JsonOptions);
        Assert.NotNull(car);
        Assert.Equal(TestUsers.UserBId, car!.OwnerId);
        Assert.Equal("userb@example.com", car.OwnerEmail);

        using var scope = server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdCar = await db.Cars.AsNoTracking().SingleAsync(c => c.Id == car.Id);
        Assert.Equal(TestUsers.UserBId, createdCar.OwnerId);
    }

    [Fact]
    public async Task CreateCar_InvalidInput_Returns400()
    {
        using var server = CreateServer();
        await SeedUserAsync(server.Services, TestUsers.UserAId, "usera@example.com");
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(TestUsers.UserAId));

        var invalid = CreateValidRequest();
        invalid.Title = string.Empty;
        invalid.Price = 0;

        var response = await client.PostAsJsonAsync("/api/cars", invalid, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCar_Unauthenticated_Returns401()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cars", CreateValidRequest(), JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task SeedUserAsync(IServiceProvider services, Guid userId, string email)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = email,
            AvatarUrl = "https://example.com/avatar.png",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = SeedCreatedAt,
            UpdatedAt = SeedUpdatedAt
        });
        await db.SaveChangesAsync();
    }

    private static TestServer CreateServer()
    {
        var databaseName = $"cars-create-{Guid.NewGuid():N}";

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

    private static CreateCarRequestDTO CreateValidRequest()
    {
        return new CreateCarRequestDTO
        {
            Title = "BMW 320i 2010 Excellent Condition",
            Brand = "BMW",
            Model = "320i",
            Description = "Used BMW in excellent condition",
            Price = 13500,
            Location = "Cairo",
            Year = 2010,
            Mileage = 145000,
            FuelType = FuelType.Gasoline,
            TransmissionType = TransmissionType.Manual,
            ImageUrls = ["https://example.com/car1.jpg"]
        };
    }

    private static string CreateJwt(Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, UserRole.User.ToString())
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

    private static class TestUsers
    {
        public static readonly Guid UserAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid UserBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
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
