namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Data Transfer Object for message information.
/// Used for transferring message data between frontend and backend.
/// </summary>
public class MessageDTO
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who sent the message.
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who received the message.
    /// </summary>
    public Guid ReceiverId { get; set; }

    /// <summary>
    /// Gets or sets optional related car identifier.
    /// </summary>
    public Guid? CarId { get; set; }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the message has been read by receiver.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets whether the message has reached at least one receiver connection.
    /// </summary>
    public bool IsDelivered { get; set; }

    /// <summary>
    /// Gets or sets when the message was delivered, in UTC.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets whether the receiver has seen the message.
    /// </summary>
    public bool IsSeen { get; set; }

    /// <summary>
    /// Gets or sets when the message was seen, in UTC.
    /// </summary>
    public DateTime? SeenAt { get; set; }

    /// <summary>
    /// Gets or sets whether the message has been soft-deleted.
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Gets or sets message creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Backward-compatible alias for CreatedAt.
    /// </summary>
    public DateTime SentAt
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }
}
