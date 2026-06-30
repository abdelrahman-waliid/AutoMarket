using CarMarketplace.Application.DTOs;
using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Repository for message persistence. Implemented in Infrastructure.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Gets conversation summaries for the current user in a single query joined with users.
    /// </summary>
    Task<List<ConversationDTO>> GetConversationSummariesAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages between two users, ordered by sent date.
    /// </summary>
    Task<List<Message>> GetBetweenUsersAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a user (sent or received), optionally filtered by content. Ordered by sent date.
    /// </summary>
    Task<List<Message>> GetForUserAsync(Guid userId, string? contentSearch = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread message count for the specified receiver.
    /// </summary>
    Task<int> CountUnreadForReceiverAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latest messages involving the specified user.
    /// </summary>
    Task<List<Message>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks unread messages as read for current user's conversation with another user.
    /// </summary>
    Task<int> MarkConversationAsReadAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks unseen messages as seen for current user's conversation with another user.
    /// </summary>
    Task<List<MessageSeenReceiptDTO>> MarkConversationAsSeenAsync(
        Guid currentUserId,
        Guid otherUserId,
        DateTime seenAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks unread messages addressed to current user as read.
    /// </summary>
    Task<int> MarkAllForUserAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks unseen messages addressed to current user as seen.
    /// </summary>
    Task<List<MessageSeenReceiptDTO>> MarkAllForUserAsSeenAsync(
        Guid currentUserId,
        DateTime seenAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks undelivered incoming messages as delivered for the receiver.
    /// </summary>
    Task<List<MessageDeliveryReceiptDTO>> MarkPendingIncomingAsDeliveredAsync(
        Guid receiverId,
        DateTime deliveredAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by id (tracked, for delete).
    /// </summary>
    Task<Message?> GetByIdTrackedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Add(Message message);

    /// <summary>
    /// Marks a message for removal. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Remove(Message message);
}
