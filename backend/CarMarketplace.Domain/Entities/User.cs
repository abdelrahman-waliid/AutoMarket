using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Domain.Entities;

/// <summary>
/// Represents a user in the car marketplace system.
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token security stamp. Updating this invalidates previously issued access tokens.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the profile avatar URL.
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failed login attempts.
    /// </summary>
    [Required]
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Gets or sets the lockout end timestamp in UTC. Null means the account is not locked.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the role of the user.
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets whether the user currently has at least one active chat connection.
    /// </summary>
    [Required]
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets when the user was last seen online, in UTC.
    /// </summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this user was soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of cars added by this user.
    /// </summary>
    [InverseProperty(nameof(Car.Owner))]
    public virtual ICollection<Car> AddedCars { get; set; } = new List<Car>();

    /// <summary>
    /// Gets or sets the collection of messages sent by this user.
    /// </summary>
    [InverseProperty(nameof(Message.Sender))]
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

    /// <summary>
    /// Gets or sets the collection of messages received by this user.
    /// </summary>
    [InverseProperty(nameof(Message.Receiver))]
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

    /// <summary>
    /// Gets or sets refresh tokens owned by this user.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Gets or sets password reset tokens owned by this user.
    /// </summary>
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    public User()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class with specified values.
    /// </summary>
    /// <param name="fullName">The full name of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="passwordHash">The hashed password of the user.</param>
    /// <param name="role">The role of the user.</param>
    public User(string fullName, string email, string passwordHash, UserRole role)
        : this()
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }
}
