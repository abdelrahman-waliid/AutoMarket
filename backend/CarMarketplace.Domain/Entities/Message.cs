using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarMarketplace.Domain.Entities;

/// <summary>
/// Represents a message between users in the car marketplace system.
/// </summary>
[Table("Messages")]
public class Message
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who sent the message.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Sender))]
    public Guid SenderId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who received the message.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Receiver))]
    public Guid ReceiverId { get; set; }

    /// <summary>
    /// Gets or sets the car linked to this message, when applicable.
    /// </summary>
    [ForeignKey(nameof(Car))]
    public Guid? CarId { get; set; }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the receiver has read the message.
    /// </summary>
    [Required]
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets whether the message has reached at least one receiver connection.
    /// </summary>
    [Required]
    public bool IsDelivered { get; set; }

    /// <summary>
    /// Gets or sets when the message was delivered, in UTC.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets whether the receiver has seen the message.
    /// </summary>
    [Required]
    public bool IsSeen { get; set; }

    /// <summary>
    /// Gets or sets when the message was seen, in UTC.
    /// </summary>
    public DateTime? SeenAt { get; set; }

    /// <summary>
    /// Gets or sets whether this message was soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the message was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Backward-compatible alias for CreatedAt.
    /// </summary>
    [NotMapped]
    public DateTime SentAt
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    /// <summary>
    /// Gets or sets the user who sent the message.
    /// </summary>
    [InverseProperty(nameof(User.SentMessages))]
    public virtual User Sender { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user who received the message.
    /// </summary>
    [InverseProperty(nameof(User.ReceivedMessages))]
    public virtual User Receiver { get; set; } = null!;

    /// <summary>
    /// Gets or sets the car referenced by this message.
    /// </summary>
    public virtual Car? Car { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> class.
    /// </summary>
    public Message()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsRead = false;
        IsDelivered = false;
        IsSeen = false;
        IsDeleted = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> class with specified values.
    /// </summary>
    /// <param name="senderId">The unique identifier of the user who sent the message.</param>
    /// <param name="receiverId">The unique identifier of the user who received the message.</param>
    /// <param name="content">The content of the message.</param>
    public Message(Guid senderId, Guid receiverId, string content)
        : this()
    {
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
    }
}
