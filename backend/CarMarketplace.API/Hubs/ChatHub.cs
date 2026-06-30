using System.Security.Claims;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CarMarketplace.API.Hubs;

/// <summary>
/// SignalR hub for realtime chat updates.
/// Note: business logic remains in MessageService; the hub is only a transport.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IMessageRealtimeNotifier _realtimeNotifier;
    private readonly IUserService _userService;
    private readonly IUserConnectionTracker _userConnectionTracker;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageService messageService,
        IMessageRealtimeNotifier realtimeNotifier,
        IUserService userService,
        IUserConnectionTracker userConnectionTracker,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _realtimeNotifier = realtimeNotifier;
        _userService = userService;
        _userConnectionTracker = userConnectionTracker;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var claimUserId = GetCurrentUserIdString();

            if (Context.UserIdentifier != null
                && !string.Equals(Context.UserIdentifier, claimUserId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "SignalR connection rejected: UserIdentifier mismatch. ClaimUserId={ClaimUserId}, UserIdentifier={UserIdentifier}",
                    claimUserId,
                    Context.UserIdentifier);

                Context.Abort();
                return;
            }

            // Each user joins a group named by their userId. This enables direct user notifications.
            await Groups.AddToGroupAsync(Context.ConnectionId, claimUserId);

            if (Guid.TryParse(claimUserId, out var userId))
            {
                await TrackOnlineAsync(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SignalR connection setup.");
            Context.Abort();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetCurrentUserIdStringOrNull();
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

                if (Guid.TryParse(userId, out var parsedUserId))
                {
                    await TrackOfflineAsync(parsedUserId, Context.ConnectionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SignalR disconnect cleanup.");
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Adds the current connection to the user's group.
    /// The client should pass its own userId only.
    /// </summary>
    public async Task JoinConversation(string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserIdString();
            if (!string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new HubException("Cannot join another user's group.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JoinConversation failed.");
            throw new HubException("Failed to join conversation.");
        }
    }

    /// <summary>
    /// Removes the current connection from the user's group.
    /// The client should pass its own userId only.
    /// </summary>
    public async Task LeaveConversation(string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserIdString();
            if (!string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new HubException("Cannot leave another user's group.");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaveConversation failed.");
            throw new HubException("Failed to leave conversation.");
        }
    }

    /// <summary>
    /// Sends a message using the existing MessageService (source of truth).
    /// The service will persist and emit realtime updates.
    /// </summary>
    public async Task<MessageDTO> SendMessage(MessageDTO messageDto)
    {
        try
        {
            return await _messageService.SendMessageAsync(messageDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "SendMessage unauthorized.");
            throw new HubException("Unauthorized.");
        }
        catch (ForbiddenAccessException ex)
        {
            _logger.LogWarning(ex, "SendMessage forbidden.");
            throw new HubException("Forbidden.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "SendMessage invalid.");
            throw new HubException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed.");
            throw new HubException("Failed to send message.");
        }
    }

    /// <summary>
    /// Optional typing indicator.
    /// </summary>
    public async Task UserTyping(Guid receiverId)
    {
        await Typing(receiverId);
    }

    /// <summary>
    /// Sends a typing indicator to another user.
    /// </summary>
    public async Task Typing(Guid toUserId)
    {
        try
        {
            var senderIdString = GetCurrentUserIdString();
            if (!Guid.TryParse(senderIdString, out var senderId))
            {
                throw new HubException("Invalid user identifier.");
            }

            await _realtimeNotifier.NotifyUserTypingAsync(toUserId, senderId);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Typing failed.");
            throw new HubException("Failed to send typing indicator.");
        }
    }

    /// <summary>
    /// Sends a stop-typing indicator to another user.
    /// </summary>
    public async Task StopTyping(Guid toUserId)
    {
        try
        {
            var senderIdString = GetCurrentUserIdString();
            if (!Guid.TryParse(senderIdString, out var senderId))
            {
                throw new HubException("Invalid user identifier.");
            }

            await _realtimeNotifier.NotifyUserStoppedTypingAsync(toUserId, senderId);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StopTyping failed.");
            throw new HubException("Failed to send stop typing indicator.");
        }
    }

    private string GetCurrentUserIdString()
    {
        var userId = GetCurrentUserIdStringOrNull();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("Authentication is required.");
        }

        return userId;
    }

    private string? GetCurrentUserIdStringOrNull()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task TrackOnlineAsync(Guid userId, string connectionId)
    {
        var becameOnline = _userConnectionTracker.AddConnection(userId, connectionId);
        if (!becameOnline)
        {
            return;
        }

        await _userService.MarkUserOnlineAsync(userId);
        await _messageService.MarkPendingMessagesDeliveredAsync(userId);
        await _realtimeNotifier.NotifyUserOnlineAsync(userId);
    }

    private async Task TrackOfflineAsync(Guid userId, string connectionId)
    {
        var becameOffline = _userConnectionTracker.RemoveConnection(userId, connectionId);
        if (!becameOffline)
        {
            return;
        }

        var lastSeen = await _userService.MarkUserOfflineAsync(userId) ?? DateTime.UtcNow;
        await _realtimeNotifier.NotifyUserOfflineAsync(userId, lastSeen);
    }
}
