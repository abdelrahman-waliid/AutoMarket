namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for user login request.
/// </summary>
public class LoginRequestDTO
{
    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password of the user.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
