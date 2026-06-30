using CarMarketplace.Application.Configuration;

namespace CarMarketplace.API.Configuration;

/// <summary>
/// Performs deployment-time JWT configuration validation.
/// </summary>
public static class JwtSettingsStartupValidator
{
    public const string DefaultSecretPlaceholder = "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLongForSecurity!";
    public const int MinimumSecretLength = 32;

    /// <summary>
    /// Validates JWT settings for the current environment.
    /// Throws <see cref="InvalidOperationException"/> on invalid configuration.
    /// </summary>
    public static void ValidateOrThrow(JwtSettings settings, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(settings.SecretKey))
        {
            throw new InvalidOperationException($"{JwtSettings.SectionName}:SecretKey is required.");
        }

        if (settings.SecretKey.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException(
                $"{JwtSettings.SectionName}:SecretKey must be at least {MinimumSecretLength} characters.");
        }

        if (!isDevelopment
            && string.Equals(settings.SecretKey, DefaultSecretPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{JwtSettings.SectionName}:SecretKey is using the default placeholder. Set a real secret via environment/secret store.");
        }
    }
}
