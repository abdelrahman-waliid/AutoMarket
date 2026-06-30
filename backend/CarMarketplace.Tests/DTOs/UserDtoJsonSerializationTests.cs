using System.Text.Json;
using System.Text.Json.Serialization;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Domain.Enums;
using Xunit;

namespace CarMarketplace.Tests.DTOs;

public class UserDtoJsonSerializationTests
{
    [Fact]
    public void UserDto_WithJsonStringEnumConverter_SerializesRoleAsString()
    {
        var userDto = new UserDTO
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = "admin@example.com",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var json = JsonSerializer.Serialize(userDto, jsonOptions);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("Admin", document.RootElement.GetProperty("role").GetString());
    }
}
