namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for login/refresh response containing user and access token.
/// </summary>
public class LoginResponseDTO
{
    /// <summary>
    /// Gets or sets the authenticated user.
    /// </summary>
    public UserDTO User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JWT access token (short-lived).
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
