namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for user registration request.
/// Role is never accepted from the client; new users are always created with UserRole.User.
/// Only Admin can assign roles via the update-user endpoint.
/// </summary>
public class RegisterRequestDTO
{
    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password of the user (will be hashed with BCrypt before storage).
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
