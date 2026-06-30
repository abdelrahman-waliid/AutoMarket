using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarMarketplace.Domain.Entities;

/// <summary>
/// Stores hashed one-time password reset tokens.
/// </summary>
[Table("PasswordResetTokens")]
public class PasswordResetToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public bool IsUsed { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
