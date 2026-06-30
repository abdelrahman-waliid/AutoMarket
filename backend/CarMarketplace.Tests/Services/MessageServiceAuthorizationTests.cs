using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Domain.Entities;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class MessageServiceAuthorizationTests
{
    private readonly Mock<IMessageRepository> _messageRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IMessageRealtimeNotifier> _realtimeNotifier = new();
    private readonly Mock<IUserConnectionTracker> _userConnectionTracker = new();
    private readonly MessageService _service;

    public MessageServiceAuthorizationTests()
    {
        _messageRepository
            .Setup(x => x.MarkConversationAsSeenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _messageRepository
            .Setup(x => x.MarkAllForUserAsSeenAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _messageRepository
            .Setup(x => x.MarkPendingIncomingAsDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _realtimeNotifier
            .Setup(x => x.NotifyMessageReceivedAsync(It.IsAny<Guid>(), It.IsAny<MessageDTO>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyMessageReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyMessageDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyMessageSeenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyMessageDeletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyMessagesReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyUserOnlineAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyUserOfflineAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyUserTypingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _realtimeNotifier
            .Setup(x => x.NotifyUserStoppedTypingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userConnectionTracker
            .Setup(x => x.IsOnline(It.IsAny<Guid>()))
            .Returns(false);

        _service = new MessageService(
            _messageRepository.Object,
            _userRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object,
            _realtimeNotifier.Object,
            _userConnectionTracker.Object);
    }

    [Fact]
    public async Task GetMessagesForUserAsync_WhenCurrentUserMatchesTarget_ReturnsMessages()
    {
        var currentUserId = Guid.NewGuid();
        var messages = new List<Message>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SenderId = currentUserId,
                ReceiverId = Guid.NewGuid(),
                Content = "Hello",
                SentAt = DateTime.UtcNow
            }
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.GetForUserAsync(currentUserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _service.GetMessagesForUserAsync(currentUserId);

        Assert.Single(result);
        Assert.Equal(currentUserId, result[0].SenderId);
    }

    [Fact]
    public async Task GetMessagesForUserAsync_WhenUnreadMessagesExist_MarksThemAsRead()
    {
        var currentUserId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var unreadMessage = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = currentUserId,
            Content = "Unread",
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var readMessage = new Message
        {
            Id = unreadMessage.Id,
            SenderId = senderId,
            ReceiverId = currentUserId,
            Content = "Unread",
            IsRead = true,
            CreatedAt = unreadMessage.CreatedAt
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.SetupSequence(x => x.GetForUserAsync(currentUserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([unreadMessage])
            .ReturnsAsync([readMessage]);
        _messageRepository.Setup(x => x.MarkAllForUserAsSeenAsync(
                currentUserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MessageSeenReceiptDTO
                {
                    MessageId = unreadMessage.Id,
                    SenderId = senderId,
                    ReceiverId = currentUserId,
                    SeenAt = DateTime.UtcNow
                }
            ]);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.GetMessagesForUserAsync(currentUserId);

        Assert.Single(result);
        Assert.True(result[0].IsRead);
        _messageRepository.Verify(
            x => x.MarkAllForUserAsSeenAsync(currentUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageSeenAsync(
                senderId,
                currentUserId,
                unreadMessage.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMessagesForUserAsync_WhenCurrentUserDoesNotMatchTarget_ThrowsForbidden()
    {
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.GetMessagesForUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetMessagesForUserAsync_WhenCurrentUserMissing_ThrowsUnauthorized()
    {
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetMessagesForUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetMessagesBetweenUsersAsync_WhenCurrentUserNotParticipant_ThrowsForbidden()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.GetMessagesBetweenUsersAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetMessagesBetweenUsersAsync_WhenCurrentUserParticipant_ReturnsMessages()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var messages = new List<Message>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SenderId = currentUserId,
                ReceiverId = otherUserId,
                Content = "Hi",
                SentAt = DateTime.UtcNow
            }
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.GetBetweenUsersAsync(currentUserId, otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _service.GetMessagesBetweenUsersAsync(currentUserId, otherUserId);

        Assert.Single(result);
        Assert.Equal(currentUserId, result[0].SenderId);
        Assert.Equal(otherUserId, result[0].ReceiverId);
    }

    [Fact]
    public async Task GetMessagesBetweenUsersAsync_WhenMessageDeleted_ReturnsDeletedFlagAndPlaceholderContent()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var deletedMessage = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "Original content",
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.GetBetweenUsersAsync(currentUserId, otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([deletedMessage]);
        _messageRepository.Setup(x => x.MarkConversationAsSeenAsync(
                currentUserId,
                otherUserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetMessagesBetweenUsersAsync(currentUserId, otherUserId);

        var message = Assert.Single(result);
        Assert.True(message.Deleted);
        Assert.Equal("Message deleted", message.Content);
    }

    [Fact]
    public async Task GetMessagesBetweenUsersAsync_MarksConversationAsRead()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var unread = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "Unread",
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        var read = new Message
        {
            Id = unread.Id,
            SenderId = otherUserId,
            ReceiverId = currentUserId,
            Content = "Unread",
            IsRead = true,
            CreatedAt = unread.CreatedAt
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.SetupSequence(x => x.GetBetweenUsersAsync(currentUserId, otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([unread])
            .ReturnsAsync([read]);
        _messageRepository.Setup(x => x.MarkConversationAsSeenAsync(
                currentUserId,
                otherUserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MessageSeenReceiptDTO
                {
                    MessageId = unread.Id,
                    SenderId = otherUserId,
                    ReceiverId = currentUserId,
                    SeenAt = DateTime.UtcNow
                }
            ]);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.GetMessagesBetweenUsersAsync(currentUserId, otherUserId);

        Assert.Single(result);
        Assert.True(result[0].IsRead);
        _messageRepository.Verify(
            x => x.MarkConversationAsSeenAsync(currentUserId, otherUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageSeenAsync(
                otherUserId,
                currentUserId,
                unread.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConversationsAsync_ReturnsUserDataAndSummaryFields()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var expected = new List<ConversationDTO>
        {
            new()
            {
                OtherUserId = otherUserId,
                OtherUserName = "Alice",
                OtherUserAvatar = "https://cdn.example.com/alice.png",
                LastMessage = "Hi",
                LastMessageAt = DateTime.UtcNow,
                UnreadCount = 2
            }
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository
            .Setup(x => x.GetConversationSummariesAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetConversationsAsync();

        var conversation = Assert.Single(result);
        Assert.Equal(otherUserId, conversation.OtherUserId);
        Assert.Equal("Alice", conversation.OtherUserName);
        Assert.Equal("https://cdn.example.com/alice.png", conversation.OtherUserAvatar);
        Assert.Equal("Hi", conversation.LastMessage);
        Assert.Equal(2, conversation.UnreadCount);
    }

    [Fact]
    public async Task SendMessageAsync_IgnoresClientSenderId_UsesCurrentUserAsSender()
    {
        var currentUserId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var fakeSenderId = Guid.NewGuid();

        Message? addedMessage = null;

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _userRepository.Setup(x => x.ExistsByIdAsync(currentUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userRepository.Setup(x => x.ExistsByIdAsync(receiverId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _messageRepository.Setup(x => x.Add(It.IsAny<Message>()))
            .Callback<Message>(msg => addedMessage = msg);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new MessageDTO
        {
            SenderId = fakeSenderId,
            ReceiverId = receiverId,
            Content = "Test content"
        };

        var result = await _service.SendMessageAsync(request);

        Assert.NotNull(addedMessage);
        Assert.Equal(currentUserId, addedMessage!.SenderId);
        Assert.NotEqual(fakeSenderId, addedMessage.SenderId);
        Assert.Equal(currentUserId, result.SenderId);
        Assert.Equal(receiverId, result.ReceiverId);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(addedMessage.Id, result.Id);
        Assert.False(result.IsDelivered);
        Assert.Null(result.DeliveredAt);
        Assert.False(result.Deleted);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeNotifier.Verify(x => x.NotifyMessageReceivedAsync(receiverId, It.Is<MessageDTO>(dto => dto.SenderId == currentUserId && dto.ReceiverId == receiverId && dto.Content == "Test content"), It.IsAny<CancellationToken>()), Times.Once);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageDeliveredAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WhenReceiverOnline_MarksDeliveredAndEmitsDeliveredEvent()
    {
        var currentUserId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        Message? addedMessage = null;

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _userRepository.Setup(x => x.ExistsByIdAsync(currentUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userRepository.Setup(x => x.ExistsByIdAsync(receiverId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userConnectionTracker.Setup(x => x.IsOnline(receiverId)).Returns(true);
        _messageRepository.Setup(x => x.Add(It.IsAny<Message>()))
            .Callback<Message>(msg => addedMessage = msg);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _service.SendMessageAsync(new MessageDTO
        {
            ReceiverId = receiverId,
            Content = "Online delivery"
        });

        Assert.NotNull(addedMessage);
        Assert.True(addedMessage!.IsDelivered);
        Assert.NotNull(addedMessage.DeliveredAt);
        Assert.True(result.IsDelivered);
        Assert.Equal(addedMessage.DeliveredAt, result.DeliveredAt);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageDeliveredAsync(
                currentUserId,
                receiverId,
                result.Id,
                result.DeliveredAt!.Value,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkPendingMessagesDeliveredAsync_EmitsOnlyUpdatedDeliveryReceipts()
    {
        var receiverId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        _messageRepository.Setup(x => x.MarkPendingIncomingAsDeliveredAsync(
                receiverId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MessageDeliveryReceiptDTO
                {
                    MessageId = messageId,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    DeliveredAt = DateTime.UtcNow
                }
            ]);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var updated = await _service.MarkPendingMessagesDeliveredAsync(receiverId);

        Assert.Equal(1, updated);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageDeliveredAsync(
                senderId,
                receiverId,
                messageId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WhenCurrentUserMissing_ThrowsUnauthorized()
    {
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.SendMessageAsync(new MessageDTO { ReceiverId = Guid.NewGuid(), Content = "x" }));
    }

    [Fact]
    public async Task SendMessageAsync_WhenReceiverIsCurrentUser_ThrowsInvalidOperation()
    {
        var currentUserId = Guid.NewGuid();
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendMessageAsync(new MessageDTO { ReceiverId = currentUserId, Content = "x" }));
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenCurrentUserIsSender_DeletesMessage()
    {
        var currentUserId = Guid.NewGuid();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = Guid.NewGuid(),
            Content = "Delete me"
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.GetByIdTrackedAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var deleted = await _service.DeleteMessageAsync(message.Id);

        Assert.True(deleted);
        Assert.True(message.IsDeleted);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageDeletedAsync(message.Id, currentUserId, message.ReceiverId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenCurrentUserIsReceiver_DeletesMessage()
    {
        var currentUserId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = currentUserId,
            Content = "Delete me"
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.GetByIdTrackedAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var deleted = await _service.DeleteMessageAsync(message.Id);

        Assert.True(deleted);
        Assert.True(message.IsDeleted);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageDeletedAsync(message.Id, senderId, currentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenCurrentUserNotParticipant_ThrowsForbidden()
    {
        var currentUserId = Guid.NewGuid();
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Private"
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.GetByIdTrackedAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.DeleteMessageAsync(message.Id));
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenMessageDoesNotExist_ReturnsFalse()
    {
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(Guid.NewGuid());
        _messageRepository.Setup(x => x.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message?)null);

        var deleted = await _service.DeleteMessageAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenMessagesUpdated_EmitsMessagesReadEvent()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.MarkConversationAsSeenAsync(
                currentUserId,
                otherUserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MessageSeenReceiptDTO { MessageId = Guid.NewGuid(), SenderId = otherUserId, ReceiverId = currentUserId, SeenAt = DateTime.UtcNow },
                new MessageSeenReceiptDTO { MessageId = Guid.NewGuid(), SenderId = otherUserId, ReceiverId = currentUserId, SeenAt = DateTime.UtcNow },
                new MessageSeenReceiptDTO { MessageId = Guid.NewGuid(), SenderId = otherUserId, ReceiverId = currentUserId, SeenAt = DateTime.UtcNow }
            ]);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var updated = await _service.MarkAsReadAsync(otherUserId);

        Assert.Equal(3, updated);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeNotifier.Verify(
            x => x.NotifyMessagesReadAsync(currentUserId, otherUserId, It.IsAny<CancellationToken>()),
            Times.Once);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageSeenAsync(
                otherUserId,
                currentUserId,
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNoMessagesUpdated_DoesNotEmitEvent()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        _messageRepository.Setup(x => x.MarkConversationAsSeenAsync(
                currentUserId,
                otherUserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var updated = await _service.MarkAsReadAsync(otherUserId);

        Assert.Equal(0, updated);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeNotifier.Verify(
            x => x.NotifyMessagesReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _realtimeNotifier.Verify(
            x => x.NotifyMessageSeenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_WhenCurrentUserMissing_ThrowsUnauthorized()
    {
        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteMessageAsync(Guid.NewGuid()));
    }
}
