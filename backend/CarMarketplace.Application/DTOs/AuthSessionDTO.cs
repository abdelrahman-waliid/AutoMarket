namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Internal authentication result used by controllers to set refresh-token cookies.
/// </summary>
public class AuthSessionDTO
{
    /// <summary>
    /// Gets or sets the API response payload (user + access token).
    /// </summary>
    public LoginResponseDTO Response { get; set; } = null!;

    /// <summary>
    /// Gets or sets the raw refresh token value to be written to HttpOnly cookie.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
