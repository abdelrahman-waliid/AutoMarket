using CarMarketplace.API.Configuration;
using CarMarketplace.Application.Configuration;
using Xunit;

namespace CarMarketplace.Tests.Configuration;

public class JwtSettingsStartupValidatorTests
{
    [Fact]
    public void ValidateOrThrow_ProductionPlaceholderSecret_Throws()
    {
        var settings = new JwtSettings
        {
            SecretKey = JwtSettingsStartupValidator.DefaultSecretPlaceholder,
            Issuer = "issuer",
            Audience = "audience",
            ExpirationInMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        Assert.Throws<InvalidOperationException>(() =>
            JwtSettingsStartupValidator.ValidateOrThrow(settings, isDevelopment: false));
    }

    [Fact]
    public void ValidateOrThrow_ShortSecret_Throws()
    {
        var settings = new JwtSettings
        {
            SecretKey = "short-secret",
            Issuer = "issuer",
            Audience = "audience",
            ExpirationInMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        Assert.Throws<InvalidOperationException>(() =>
            JwtSettingsStartupValidator.ValidateOrThrow(settings, isDevelopment: true));
    }

    [Fact]
    public void ValidateOrThrow_ProductionStrongSecret_Passes()
    {
        var settings = new JwtSettings
        {
            SecretKey = "0123456789ABCDEF0123456789ABCDEF",
            Issuer = "issuer",
            Audience = "audience",
            ExpirationInMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        JwtSettingsStartupValidator.ValidateOrThrow(settings, isDevelopment: false);
    }
}
