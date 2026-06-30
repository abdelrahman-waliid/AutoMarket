namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Represents a message seen status update.
/// </summary>
public class MessageSeenReceiptDTO
{
    public Guid MessageId { get; set; }

    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public DateTime SeenAt { get; set; }
}
