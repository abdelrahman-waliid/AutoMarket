using CarMarketplace.Domain.Enums;
using System.Text.Json.Serialization;

namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for user information.
/// Used for transferring user data between frontend and backend without exposing password.
/// </summary>
public class UserDTO
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role of the user.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets profile avatar URL.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the user currently has an active chat connection.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets when the user was last seen online, in UTC.
    /// </summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>
    /// Gets or sets the token security stamp. Hidden from API JSON responses.
    /// </summary>
    [JsonIgnore]
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets updated timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
