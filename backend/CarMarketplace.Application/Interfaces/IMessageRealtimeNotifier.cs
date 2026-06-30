using CarMarketplace.Application.DTOs;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Abstraction for sending message-related realtime notifications.
/// Implemented in the API layer using SignalR.
/// </summary>
public interface IMessageRealtimeNotifier
{
    Task NotifyMessageReceivedAsync(Guid receiverId, MessageDTO messageDto, CancellationToken cancellationToken = default);

    Task NotifyMessageReadAsync(Guid senderId, Guid messageId, CancellationToken cancellationToken = default);

    Task NotifyMessageDeliveredAsync(
        Guid senderId,
        Guid receiverId,
        Guid messageId,
        DateTime deliveredAt,
        CancellationToken cancellationToken = default);

    Task NotifyMessageSeenAsync(
        Guid senderId,
        Guid receiverId,
        Guid messageId,
        DateTime seenAt,
        CancellationToken cancellationToken = default);

    Task NotifyMessageDeletedAsync(Guid messageId, Guid senderId, Guid receiverId, CancellationToken cancellationToken = default);

    Task NotifyMessagesReadAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default);

    Task NotifyUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default);

    Task NotifyUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default);

    Task NotifyUserOfflineAsync(Guid userId, DateTime lastSeen, CancellationToken cancellationToken = default);

    Task NotifyUserTypingAsync(Guid receiverId, Guid senderId, CancellationToken cancellationToken = default);

    Task NotifyUserStoppedTypingAsync(Guid receiverId, Guid senderId, CancellationToken cancellationToken = default);
}
