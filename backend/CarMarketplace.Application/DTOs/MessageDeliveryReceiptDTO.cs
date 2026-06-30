namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Represents a message delivery status update.
/// </summary>
public class MessageDeliveryReceiptDTO
{
    public Guid MessageId { get; set; }

    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public DateTime DeliveredAt { get; set; }
}
