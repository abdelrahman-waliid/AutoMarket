using CarMarketplace.Application.DTOs;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Generates JWT tokens for authenticated users. Implemented in Infrastructure.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the given user (symmetric key, claims: NameIdentifier, userId, Role, Email, Name).
    /// </summary>
    /// <param name="user">The user DTO.</param>
    /// <returns>The signed JWT token string.</returns>
    string GenerateToken(UserDTO user);
}
