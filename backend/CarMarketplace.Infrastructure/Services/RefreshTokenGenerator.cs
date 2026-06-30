using System.Security.Cryptography;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.Infrastructure.Services;

/// <summary>
/// Generates cryptographically secure random strings for refresh tokens.
/// </summary>
public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenByteLength = 64;

    /// <inheritdoc/>
    public string Generate()
    {
        var bytes = new byte[TokenByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
