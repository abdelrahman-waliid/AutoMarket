using CarMarketplace.API.Hubs;
using CarMarketplace.API.Services;
using CarMarketplace.Application.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class SignalRMessageRealtimeNotifierTests
{
    [Fact]
    public async Task NotifyMessageReceivedAsync_SendsReceiveMessageToReceiverAndSenderGroups()
    {
        var receiverId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var dto = new MessageDTO
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        var receiverProxy = new Mock<IClientProxy>();
        receiverProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var senderProxy = new Mock<IClientProxy>();
        senderProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(receiverId.ToString())).Returns(receiverProxy.Object);
        clients.Setup(x => x.Group(senderId.ToString())).Returns(senderProxy.Object);

        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        var notifier = new SignalRMessageRealtimeNotifier(
            hubContext.Object,
            Mock.Of<ILogger<SignalRMessageRealtimeNotifier>>());

        await notifier.NotifyMessageReceivedAsync(receiverId, dto);

        receiverProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], dto)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        senderProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], dto)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyMessageDeletedAsync_SendsMessageDeletedToSenderAndReceiverGroups()
    {
        var messageId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        var senderProxy = new Mock<IClientProxy>();
        senderProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var receiverProxy = new Mock<IClientProxy>();
        receiverProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(senderId.ToString())).Returns(senderProxy.Object);
        clients.Setup(x => x.Group(receiverId.ToString())).Returns(receiverProxy.Object);

        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        var notifier = new SignalRMessageRealtimeNotifier(
            hubContext.Object,
            Mock.Of<ILogger<SignalRMessageRealtimeNotifier>>());

        await notifier.NotifyMessageDeletedAsync(messageId, senderId, receiverId);

        senderProxy.Verify(
            x => x.SendCoreAsync(
                "MessageDeleted",
                It.Is<object?[]>(args => args.Length == 1 && PayloadContainsUserId(args[0], "messageId", messageId)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        receiverProxy.Verify(
            x => x.SendCoreAsync(
                "MessageDeleted",
                It.Is<object?[]>(args => args.Length == 1 && PayloadContainsUserId(args[0], "messageId", messageId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyMessagesReadAsync_SendsMessagesReadToTargetGroupWithUserIdPayload()
    {
        var userId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var targetProxy = new Mock<IClientProxy>();
        targetProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(targetUserId.ToString())).Returns(targetProxy.Object);

        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        var notifier = new SignalRMessageRealtimeNotifier(
            hubContext.Object,
            Mock.Of<ILogger<SignalRMessageRealtimeNotifier>>());

        await notifier.NotifyMessagesReadAsync(userId, targetUserId);

        targetProxy.Verify(
            x => x.SendCoreAsync(
                "MessagesRead",
                It.Is<object?[]>(args => args.Length == 1 && PayloadContainsUserId(args[0], "userId", userId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyMessageDeliveredAsync_SendsMessageDeliveredToSenderGroup()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var deliveredAt = DateTime.UtcNow;

        var senderProxy = CreateClientProxy();
        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(senderId.ToString())).Returns(senderProxy.Object);

        var notifier = CreateNotifier(clients);

        await notifier.NotifyMessageDeliveredAsync(senderId, receiverId, messageId, deliveredAt);

        senderProxy.Verify(
            x => x.SendCoreAsync(
                "MessageDelivered",
                It.Is<object?[]>(args =>
                    args.Length == 1
                    && PayloadContainsUserId(args[0], "messageId", messageId)
                    && PayloadContainsDateTime(args[0], "deliveredAt", deliveredAt)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyMessageSeenAsync_SendsMessageSeenToSenderGroup()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var seenAt = DateTime.UtcNow;

        var senderProxy = CreateClientProxy();
        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(senderId.ToString())).Returns(senderProxy.Object);

        var notifier = CreateNotifier(clients);

        await notifier.NotifyMessageSeenAsync(senderId, receiverId, messageId, seenAt);

        senderProxy.Verify(
            x => x.SendCoreAsync(
                "MessageSeen",
                It.Is<object?[]>(args =>
                    args.Length == 1
                    && PayloadContainsUserId(args[0], "messageId", messageId)
                    && PayloadContainsDateTime(args[0], "seenAt", seenAt)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PresenceNotifications_SendOnlineAndOfflinePayloads()
    {
        var userId = Guid.NewGuid();
        var lastSeen = DateTime.UtcNow;
        var allProxy = CreateClientProxy();
        var clients = new Mock<IHubClients>();
        clients.SetupGet(x => x.All).Returns(allProxy.Object);

        var notifier = CreateNotifier(clients);

        await notifier.NotifyUserOnlineAsync(userId);
        await notifier.NotifyUserOfflineAsync(userId, lastSeen);

        allProxy.Verify(
            x => x.SendCoreAsync(
                "UserOnline",
                It.Is<object?[]>(args => args.Length == 1 && PayloadContainsUserId(args[0], "userId", userId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        allProxy.Verify(
            x => x.SendCoreAsync(
                "UserOffline",
                It.Is<object?[]>(args =>
                    args.Length == 1
                    && PayloadContainsUserId(args[0], "userId", userId)
                    && PayloadContainsDateTime(args[0], "lastSeen", lastSeen)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TypingNotifications_SendTypingAndStopTypingPayloads()
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var receiverProxy = CreateClientProxy();
        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(receiverId.ToString())).Returns(receiverProxy.Object);

        var notifier = CreateNotifier(clients);

        await notifier.NotifyUserTypingAsync(receiverId, senderId);
        await notifier.NotifyUserStoppedTypingAsync(receiverId, senderId);

        receiverProxy.Verify(
            x => x.SendCoreAsync(
                "Typing",
                It.Is<object?[]>(args =>
                    args.Length == 1
                    && PayloadContainsUserId(args[0], "fromUserId", senderId)
                    && PayloadContainsUserId(args[0], "toUserId", receiverId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        receiverProxy.Verify(
            x => x.SendCoreAsync(
                "StopTyping",
                It.Is<object?[]>(args =>
                    args.Length == 1
                    && PayloadContainsUserId(args[0], "fromUserId", senderId)
                    && PayloadContainsUserId(args[0], "toUserId", receiverId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static SignalRMessageRealtimeNotifier CreateNotifier(Mock<IHubClients> clients)
    {
        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        return new SignalRMessageRealtimeNotifier(
            hubContext.Object,
            Mock.Of<ILogger<SignalRMessageRealtimeNotifier>>());
    }

    private static Mock<IClientProxy> CreateClientProxy()
    {
        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return proxy;
    }

    private static bool PayloadContainsUserId(object? payload, string propertyName, Guid expected)
    {
        if (payload is null)
        {
            return false;
        }

        var property = payload.GetType().GetProperty(propertyName);
        if (property is null)
        {
            return false;
        }

        return property.GetValue(payload) is Guid actual && actual == expected;
    }

    private static bool PayloadContainsDateTime(object? payload, string propertyName, DateTime expected)
    {
        if (payload is null)
        {
            return false;
        }

        var property = payload.GetType().GetProperty(propertyName);
        if (property is null)
        {
            return false;
        }

        return property.GetValue(payload) is DateTime actual && actual == expected;
    }
}
