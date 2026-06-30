using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarMarketplace.Domain.Entities;

/// <summary>
/// Stores a refresh token for issuing new access tokens without re-authentication.
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the refresh token.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user id this token belongs to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the refresh token value.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this refresh token was created (UTC).
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiry date of the token (UTC).
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Backward-compatible alias for ExpiresAt.
    /// </summary>
    [NotMapped]
    public DateTime ExpiryDate
    {
        get => ExpiresAt;
        set => ExpiresAt = value;
    }

    /// <summary>
    /// Gets or sets whether the token has been revoked (e.g. on logout).
    /// </summary>
    [Required]
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets when the token was revoked (UTC).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets optional revocation reason (e.g. Rotated, Logout, ReuseDetected).
    /// </summary>
    [MaxLength(100)]
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Gets or sets the hash of the token that replaced this token during rotation.
    /// </summary>
    [MaxLength(128)]
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Gets or sets the user this token belongs to.
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Returns true if the token is valid (not revoked and not expired).
    /// </summary>
    public bool IsValid => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}
