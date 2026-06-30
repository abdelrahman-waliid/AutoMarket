using CarMarketplace.API.Hubs;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CarMarketplace.API.Services;

/// <summary>
/// SignalR implementation of <see cref="IMessageRealtimeNotifier"/>.
/// </summary>
public sealed class SignalRMessageRealtimeNotifier : IMessageRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRMessageRealtimeNotifier> _logger;

    public SignalRMessageRealtimeNotifier(IHubContext<ChatHub> hubContext, ILogger<SignalRMessageRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyMessageReceivedAsync(Guid receiverId, MessageDTO messageDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var receiverTask = _hubContext
                .Clients
                .Group(receiverId.ToString())
                .SendAsync("ReceiveMessage", messageDto, cancellationToken);

            var senderTask = _hubContext
                .Clients
                .Group(messageDto.SenderId.ToString())
                .SendAsync("ReceiveMessage", messageDto, cancellationToken);

            await Task.WhenAll(receiverTask, senderTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR ReceiveMessage event. ReceiverId={ReceiverId} MessageId={MessageId}",
                receiverId,
                messageDto.Id);
        }
    }

    public async Task NotifyMessageReadAsync(Guid senderId, Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext
                .Clients
                .Group(senderId.ToString())
                .SendAsync("MessageRead", messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR MessageRead event. SenderId={SenderId} MessageId={MessageId}",
                senderId,
                messageId);
        }
    }

    public async Task NotifyMessageDeliveredAsync(
        Guid senderId,
        Guid receiverId,
        Guid messageId,
        DateTime deliveredAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext
                .Clients
                .Group(senderId.ToString())
                .SendAsync("MessageDelivered", new { messageId, deliveredAt }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR MessageDelivered event. SenderId={SenderId} ReceiverId={ReceiverId} MessageId={MessageId}",
                senderId,
                receiverId,
                messageId);
        }
    }

    public async Task NotifyMessageSeenAsync(
        Guid senderId,
        Guid receiverId,
        Guid messageId,
        DateTime seenAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var seenTask = _hubContext
                .Clients
                .Group(senderId.ToString())
                .SendAsync("MessageSeen", new { messageId, seenAt }, cancellationToken);

            var legacyReadTask = _hubContext
                .Clients
                .Group(senderId.ToString())
                .SendAsync("MessageRead", messageId, cancellationToken);

            await Task.WhenAll(seenTask, legacyReadTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR MessageSeen event. SenderId={SenderId} ReceiverId={ReceiverId} MessageId={MessageId}",
                senderId,
                receiverId,
                messageId);
        }
    }

    public async Task NotifyMessageDeletedAsync(Guid messageId, Guid senderId, Guid receiverId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { messageId };

            var senderTask = _hubContext
                .Clients
                .Group(senderId.ToString())
                .SendAsync("MessageDeleted", payload, cancellationToken);

            var receiverTask = _hubContext
                .Clients
                .Group(receiverId.ToString())
                .SendAsync("MessageDeleted", payload, cancellationToken);

            await Task.WhenAll(senderTask, receiverTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR MessageDeleted event. MessageId={MessageId} SenderId={SenderId} ReceiverId={ReceiverId}",
                messageId,
                senderId,
                receiverId);
        }
    }

    public async Task NotifyMessagesReadAsync(Guid userId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext
                .Clients
                .Group(targetUserId.ToString())
                .SendAsync("MessagesRead", new { userId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR MessagesRead event. UserId={UserId} TargetUserId={TargetUserId}",
                userId,
                targetUserId);
        }
    }

    public async Task NotifyUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext
                .Clients
                .All
                .SendAsync("UserOnline", new { userId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR UserOnline event. UserId={UserId}", userId);
        }
    }

    public async Task NotifyUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await NotifyUserOfflineAsync(userId, DateTime.UtcNow, cancellationToken);
    }

    public async Task NotifyUserOfflineAsync(Guid userId, DateTime lastSeen, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext
                .Clients
                .All
                .SendAsync("UserOffline", new { userId, lastSeen }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR UserOffline event. UserId={UserId}", userId);
        }
    }

    public async Task NotifyUserTypingAsync(Guid receiverId, Guid senderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var typingTask = _hubContext
                .Clients
                .Group(receiverId.ToString())
                .SendAsync("Typing", new { fromUserId = senderId, toUserId = receiverId }, cancellationToken);

            var legacyTypingTask = _hubContext
                .Clients
                .Group(receiverId.ToString())
                .SendAsync("UserTyping", senderId, cancellationToken);

            await Task.WhenAll(typingTask, legacyTypingTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR UserTyping event. ReceiverId={ReceiverId} SenderId={SenderId}",
                receiverId,
                senderId);
        }
    }

    public async Task NotifyUserStoppedTypingAsync(Guid receiverId, Guid senderId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext
                .Clients
                .Group(receiverId.ToString())
                .SendAsync("StopTyping", new { fromUserId = senderId, toUserId = receiverId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SignalR StopTyping event. ReceiverId={ReceiverId} SenderId={SenderId}",
                receiverId,
                senderId);
        }
    }
}
