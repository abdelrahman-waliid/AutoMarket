using Microsoft.EntityFrameworkCore;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Infrastructure.Data;

namespace CarMarketplace.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMessageRepository"/> using <see cref="AppDbContext"/>.
/// </summary>
public class MessageRepository : IMessageRepository
{
    private const string DeletedMessageContent = "Message deleted";
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<ConversationDTO>> GetConversationSummariesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var conversationMessages = _context.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .Select(m => new
            {
                OtherUserId = m.SenderId == currentUserId ? m.ReceiverId : m.SenderId,
                m.ReceiverId,
                m.IsSeen,
                m.IsDeleted,
                m.Content,
                m.CreatedAt,
                m.Id
            });

        return await (
            from summary in conversationMessages
                .GroupBy(message => message.OtherUserId)
                .Select(conversationGroup => new
                {
                    OtherUserId = conversationGroup.Key,
                    LastMessage = conversationGroup
                        .OrderByDescending(message => message.CreatedAt)
                        .ThenByDescending(message => message.Id)
                        .Select(message => message.IsDeleted ? DeletedMessageContent : message.Content)
                        .FirstOrDefault() ?? string.Empty,
                    LastMessageAt = conversationGroup
                        .OrderByDescending(message => message.CreatedAt)
                        .ThenByDescending(message => message.Id)
                        .Select(message => message.CreatedAt)
                        .FirstOrDefault(),
                    UnreadCount = conversationGroup.Count(message => message.ReceiverId == currentUserId && !message.IsSeen && !message.IsDeleted)
                })
            join user in _context.Users.AsNoTracking() on summary.OtherUserId equals user.Id
            orderby summary.LastMessageAt descending
            select new ConversationDTO
            {
                OtherUserId = summary.OtherUserId,
                OtherUserName = user.FullName,
                OtherUserAvatar = user.AvatarUrl,
                LastMessage = summary.LastMessage,
                LastMessageAt = summary.LastMessageAt,
                UnreadCount = summary.UnreadCount,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Message>> GetBetweenUsersAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .AsNoTracking()
            .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                        (m.SenderId == user2Id && m.ReceiverId == user1Id))
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Message>> GetForUserAsync(Guid userId, string? contentSearch = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId);

        if (!string.IsNullOrWhiteSpace(contentSearch))
            query = query.Where(m => m.Content.Contains(contentSearch));

        return await query
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountUnreadForReceiverAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .AsNoTracking()
            .CountAsync(m => m.ReceiverId == userId && !m.IsSeen && !m.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Message>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default)
    {
        var recentMessages = _context.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(take);

        return await recentMessages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> MarkConversationAsReadAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        var seenReceipts = await MarkConversationAsSeenAsync(currentUserId, otherUserId, DateTime.UtcNow, cancellationToken);
        return seenReceipts.Count;
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllForUserAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var seenReceipts = await MarkAllForUserAsSeenAsync(currentUserId, DateTime.UtcNow, cancellationToken);
        return seenReceipts.Count;
    }

    /// <inheritdoc/>
    public async Task<List<MessageSeenReceiptDTO>> MarkConversationAsSeenAsync(
        Guid currentUserId,
        Guid otherUserId,
        DateTime seenAt,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _context.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == otherUserId
                        && m.ReceiverId == currentUserId
                        && !m.IsSeen
                        && !m.IsDeleted)
            .Select(message => new MessageSeenReceiptDTO
            {
                MessageId = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                SeenAt = seenAt
            })
            .ToListAsync(cancellationToken);

        await ApplySeenUpdateAsync(receipts.Select(receipt => receipt.MessageId).ToArray(), seenAt, cancellationToken);
        return receipts;
    }

    /// <inheritdoc/>
    public async Task<List<MessageSeenReceiptDTO>> MarkAllForUserAsSeenAsync(
        Guid currentUserId,
        DateTime seenAt,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ReceiverId == currentUserId && !m.IsSeen && !m.IsDeleted)
            .Select(message => new MessageSeenReceiptDTO
            {
                MessageId = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                SeenAt = seenAt
            })
            .ToListAsync(cancellationToken);

        await ApplySeenUpdateAsync(receipts.Select(receipt => receipt.MessageId).ToArray(), seenAt, cancellationToken);
        return receipts;
    }

    /// <inheritdoc/>
    public async Task<List<MessageDeliveryReceiptDTO>> MarkPendingIncomingAsDeliveredAsync(
        Guid receiverId,
        DateTime deliveredAt,
        CancellationToken cancellationToken = default)
    {
        var receipts = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ReceiverId == receiverId && !m.IsDelivered && !m.IsDeleted)
            .Select(message => new MessageDeliveryReceiptDTO
            {
                MessageId = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                DeliveredAt = deliveredAt
            })
            .ToListAsync(cancellationToken);

        await ApplyDeliveredUpdateAsync(receipts.Select(receipt => receipt.MessageId).ToArray(), deliveredAt, cancellationToken);
        return receipts;
    }

    /// <inheritdoc/>
    public async Task<Message?> GetByIdTrackedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(Message message)
    {
        _context.Messages.Add(message);
    }

    /// <inheritdoc/>
    public void Remove(Message message)
    {
        _context.Messages.Remove(message);
    }

    private async Task ApplySeenUpdateAsync(
        IReadOnlyCollection<Guid> messageIds,
        DateTime seenAt,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return;
        }

        if (_context.Database.IsRelational())
        {
            await _context.Messages
                .Where(message => messageIds.Contains(message.Id) && !message.IsSeen && !message.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.IsRead, true)
                        .SetProperty(message => message.IsSeen, true)
                        .SetProperty(message => message.SeenAt, seenAt),
                    cancellationToken);
            return;
        }

        var messages = await _context.Messages
            .Where(message => messageIds.Contains(message.Id) && !message.IsSeen && !message.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.IsRead = true;
            message.IsSeen = true;
            message.SeenAt = seenAt;
        }
    }

    private async Task ApplyDeliveredUpdateAsync(
        IReadOnlyCollection<Guid> messageIds,
        DateTime deliveredAt,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return;
        }

        if (_context.Database.IsRelational())
        {
            await _context.Messages
                .Where(message => messageIds.Contains(message.Id) && !message.IsDelivered && !message.IsDeleted)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.IsDelivered, true)
                        .SetProperty(message => message.DeliveredAt, deliveredAt),
                    cancellationToken);
            return;
        }

        var messages = await _context.Messages
            .Where(message => messageIds.Contains(message.Id) && !message.IsDelivered && !message.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.IsDelivered = true;
            message.DeliveredAt = deliveredAt;
        }
    }
}
