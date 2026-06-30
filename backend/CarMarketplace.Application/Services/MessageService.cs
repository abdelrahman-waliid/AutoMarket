using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Services;

/// <summary>
/// Service implementation for message operations.
/// </summary>
public class MessageService : IMessageService
{
    private const string DeletedMessageContent = "Message deleted";
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageRealtimeNotifier _realtimeNotifier;
    private readonly IUserConnectionTracker _userConnectionTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageService"/> class.
    /// </summary>
    /// <param name="messageRepository">The message repository.</param>
    /// <param name="userRepository">The user repository (for sender/receiver validation).</param>
    /// <param name="unitOfWork">The unit of work for persisting changes.</param>
    /// <param name="currentUserService">The current user accessor.</param>
    /// <param name="realtimeNotifier">Realtime notifier for SignalR updates.</param>
    /// <param name="userConnectionTracker">Realtime connection tracker for delivery decisions.</param>
    public MessageService(
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageRealtimeNotifier realtimeNotifier,
        IUserConnectionTracker? userConnectionTracker = null)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _realtimeNotifier = realtimeNotifier;
        _userConnectionTracker = userConnectionTracker ?? OfflineUserConnectionTracker.Instance;
    }

    /// <inheritdoc/>
    public async Task<List<ConversationDTO>> GetConversationsAsync()
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var conversations = await _messageRepository.GetConversationSummariesAsync(currentUserId);
        foreach (var conversation in conversations)
        {
            conversation.IsOnline = _userConnectionTracker.IsOnline(conversation.OtherUserId) || conversation.IsOnline;
        }

        return conversations;
    }

    /// <inheritdoc/>
    public async Task<List<MessageDTO>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        if (user1Id != currentUserId && user2Id != currentUserId)
        {
            throw new ForbiddenAccessException("You can only view conversations that involve your account.");
        }

        var messages = await _messageRepository.GetBetweenUsersAsync(user1Id, user2Id);
        var otherUserId = user1Id == currentUserId ? user2Id : user1Id;

        var seenAt = DateTime.UtcNow;
        var seenReceipts = await _messageRepository.MarkConversationAsSeenAsync(currentUserId, otherUserId, seenAt);
        if (seenReceipts.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            await Task.WhenAll(seenReceipts.Select(SafeNotifyMessageSeenAsync));
            await SafeNotifyMessagesReadAsync(currentUserId, otherUserId);

            messages = await _messageRepository.GetBetweenUsersAsync(user1Id, user2Id);
        }

        return messages.Select(MapToDTO).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<MessageDTO>> GetMessagesForUserAsync(Guid userId)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        if (userId != currentUserId)
        {
            throw new ForbiddenAccessException("You can only view your own messages.");
        }

        var messages = await _messageRepository.GetForUserAsync(userId);

        var seenAt = DateTime.UtcNow;
        var seenReceipts = await _messageRepository.MarkAllForUserAsSeenAsync(currentUserId, seenAt);
        if (seenReceipts.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            await Task.WhenAll(seenReceipts.Select(SafeNotifyMessageSeenAsync));

            messages = await _messageRepository.GetForUserAsync(userId);
        }

        return messages.Select(MapToDTO).ToList();
    }

    /// <inheritdoc/>
    public async Task<MessageDTO> SendMessageAsync(MessageDTO messageDto)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        if (messageDto.ReceiverId == currentUserId)
        {
            throw new InvalidOperationException("Sender and receiver must be different.");
        }

        var senderExists = await _userRepository.ExistsByIdAsync(currentUserId);
        var receiverExists = await _userRepository.ExistsByIdAsync(messageDto.ReceiverId);

        if (!senderExists || !receiverExists)
        {
            throw new InvalidOperationException("Sender or receiver does not exist.");
        }

        var now = DateTime.UtcNow;
        var isReceiverOnline = _userConnectionTracker.IsOnline(messageDto.ReceiverId);
        var deliveredAt = isReceiverOnline ? now : (DateTime?)null;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = messageDto.ReceiverId,
            CarId = messageDto.CarId,
            Content = messageDto.Content,
            CreatedAt = now,
            IsRead = false,
            IsDelivered = isReceiverOnline,
            DeliveredAt = deliveredAt,
            IsSeen = false,
            SeenAt = null
        };

        _messageRepository.Add(message);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDTO(message);
        await SafeNotifyMessageReceivedAsync(message.ReceiverId, dto);
        if (deliveredAt.HasValue)
        {
            await SafeNotifyMessageDeliveredAsync(new MessageDeliveryReceiptDTO
            {
                MessageId = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                DeliveredAt = deliveredAt.Value
            });
        }

        return dto;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteMessageAsync(Guid messageId)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var message = await _messageRepository.GetByIdTrackedAsync(messageId);
        if (message == null)
        {
            return false;
        }

        if (message.SenderId != currentUserId && message.ReceiverId != currentUserId)
        {
            throw new ForbiddenAccessException("You can only delete messages that involve your account.");
        }

        if (message.IsDeleted)
        {
            return true;
        }

        message.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync();
        await SafeNotifyMessageDeletedAsync(message.Id, message.SenderId, message.ReceiverId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> MarkAsReadAsync(Guid userId)
    {
        var currentUserId = GetCurrentUserIdOrThrow();

        var seenAt = DateTime.UtcNow;
        var seenReceipts = await _messageRepository.MarkConversationAsSeenAsync(currentUserId, userId, seenAt);
        if (seenReceipts.Count <= 0)
        {
            return 0;
        }

        await _unitOfWork.SaveChangesAsync();
        await Task.WhenAll(seenReceipts.Select(SafeNotifyMessageSeenAsync));
        await SafeNotifyMessagesReadAsync(currentUserId, userId);
        return seenReceipts.Count;
    }

    /// <inheritdoc/>
    public async Task<int> MarkPendingMessagesDeliveredAsync(Guid receiverId)
    {
        if (receiverId == Guid.Empty)
        {
            return 0;
        }

        var deliveredAt = DateTime.UtcNow;
        var deliveryReceipts = await _messageRepository.MarkPendingIncomingAsDeliveredAsync(receiverId, deliveredAt);
        if (deliveryReceipts.Count <= 0)
        {
            return 0;
        }

        await _unitOfWork.SaveChangesAsync();
        await Task.WhenAll(deliveryReceipts.Select(SafeNotifyMessageDeliveredAsync));
        return deliveryReceipts.Count;
    }

    private static MessageDTO MapToDTO(Message message)
    {
        var isSeen = message.IsSeen || message.IsRead;

        return new MessageDTO
        {
            Id = message.Id,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            CarId = message.CarId,
            Content = message.IsDeleted ? DeletedMessageContent : message.Content,
            IsRead = isSeen,
            IsDelivered = message.IsDelivered,
            DeliveredAt = message.DeliveredAt,
            IsSeen = isSeen,
            SeenAt = message.SeenAt,
            Deleted = message.IsDeleted,
            CreatedAt = message.CreatedAt
        };
    }

    private Guid GetCurrentUserIdOrThrow()
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return currentUserId.Value;
    }

    private async Task SafeNotifyMessageReceivedAsync(Guid receiverId, MessageDTO messageDto)
    {
        try
        {
            var task = _realtimeNotifier.NotifyMessageReceivedAsync(receiverId, messageDto);
            if (task != null)
            {
                await task;
            }
        }
        catch
        {
            // Do not fail REST calls if realtime notifications fail.
        }
    }

    private async Task SafeNotifyMessageReadAsync(Guid senderId, Guid messageId)
    {
        try
        {
            var task = _realtimeNotifier.NotifyMessageReadAsync(senderId, messageId);
            if (task != null)
            {
                await task;
            }
        }
        catch
        {
            // Do not fail REST calls if realtime notifications fail.
        }
    }

    private async Task SafeNotifyMessageDeliveredAsync(MessageDeliveryReceiptDTO receipt)
    {
        try
        {
            var task = _realtimeNotifier.NotifyMessageDeliveredAsync(
                receipt.SenderId,
                receipt.ReceiverId,
                receipt.MessageId,
                receipt.DeliveredAt);
            if (task != null)
            {
                await task;
            }
        }
        catch
        {
            // Do not fail REST calls if realtime notifications fail.
        }
    }

    private async Task SafeNotifyMessageSeenAsync(MessageSeenReceiptDTO receipt)
    {
        try
        {
            var task = _realtimeNotifier.NotifyMessageSeenAsync(
                receipt.SenderId,
                receipt.ReceiverId,
                receipt.MessageId,
                receipt.SeenAt);
            if (task != null)
            {
                await task;
            }
        }
        catch
        {
            // Do not fail REST calls if realtime notifications fail.
        }
    }

    private async Task SafeNotifyMessageDeletedAsync(Guid messageId, Guid senderId, Guid receiverId)
    {
        try
        {
            var task = _realtimeNotifier.NotifyMessageDeletedAsync(messageId, senderId, receiverId);
            if (task != null)
            {
                await task;
            }
        }
        catch
        {
            // Do not fail REST calls if realtime notifications fail.
        }
    }

    private async Task SafeNotifyMessagesReadAsync(Guid userId, Guid targetUserId)
    {
        try
        {
            var task = _realtimeNotifier.NotifyMessagesReadAsync(userId, targetUserId);
            if (task != null)
            {
                await task;
            }
        }
        catch
        {
            // Do not fail REST calls if realtime notifications fail.
        }
    }

    private sealed class OfflineUserConnectionTracker : IUserConnectionTracker
    {
        public static OfflineUserConnectionTracker Instance { get; } = new();

        public bool AddConnection(Guid userId, string connectionId) => false;

        public bool RemoveConnection(Guid userId, string connectionId) => false;

        public bool IsOnline(Guid userId) => false;

        public IReadOnlyCollection<string> GetConnectionIds(Guid userId) => [];
    }
}
