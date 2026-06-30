using CarMarketplace.API.Controllers;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Controllers;

public class MessagesControllerAuthorizationTests
{
    private readonly Mock<IMessageService> _messageService = new();
    private readonly MessagesController _controller;

    public MessagesControllerAuthorizationTests()
    {
        _controller = new MessagesController(_messageService.Object);
    }

    [Fact]
    public async Task GetMessagesForUser_WhenServiceThrowsUnauthorized_Returns401()
    {
        var userId = Guid.NewGuid();
        _messageService.Setup(x => x.GetMessagesForUserAsync(userId))
            .ThrowsAsync(new UnauthorizedAccessException("Authentication is required."));

        var result = await _controller.GetMessagesForUser(userId);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetMessagesForUser_WhenServiceThrowsForbidden_Returns403()
    {
        var userId = Guid.NewGuid();
        _messageService.Setup(x => x.GetMessagesForUserAsync(userId))
            .ThrowsAsync(new ForbiddenAccessException());

        var result = await _controller.GetMessagesForUser(userId);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetMessagesBetweenUsers_WhenServiceThrowsForbidden_Returns403()
    {
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        _messageService.Setup(x => x.GetMessagesBetweenUsersAsync(user1Id, user2Id))
            .ThrowsAsync(new ForbiddenAccessException());

        var result = await _controller.GetMessagesBetweenUsers(user1Id, user2Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetConversations_WhenServiceReturnsData_IncludesUserFields()
    {
        var conversations = new List<ConversationDTO>
        {
            new()
            {
                OtherUserId = Guid.NewGuid(),
                OtherUserName = "Alice",
                OtherUserAvatar = "https://cdn.example.com/alice.png",
                LastMessage = "Hi",
                LastMessageAt = DateTime.UtcNow,
                UnreadCount = 1
            }
        };

        _messageService.Setup(x => x.GetConversationsAsync())
            .ReturnsAsync(conversations);

        var result = await _controller.GetConversations();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<List<ConversationDTO>>(ok.Value);
        var item = Assert.Single(payload);
        Assert.Equal("Alice", item.OtherUserName);
        Assert.Equal("https://cdn.example.com/alice.png", item.OtherUserAvatar);
    }

    [Fact]
    public async Task SendMessage_WhenServiceThrowsUnauthorized_Returns401()
    {
        var request = new MessageDTO
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Hello"
        };

        _messageService.Setup(x => x.SendMessageAsync(request))
            .ThrowsAsync(new UnauthorizedAccessException("Authentication is required."));

        var result = await _controller.SendMessage(request);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_WhenServiceThrowsForbidden_Returns403()
    {
        var messageId = Guid.NewGuid();
        _messageService.Setup(x => x.DeleteMessageAsync(messageId))
            .ThrowsAsync(new ForbiddenAccessException());

        var result = await _controller.DeleteMessage(messageId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteMessage_WhenServiceReturnsFalse_Returns404()
    {
        var messageId = Guid.NewGuid();
        _messageService.Setup(x => x.DeleteMessageAsync(messageId)).ReturnsAsync(false);

        var result = await _controller.DeleteMessage(messageId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceThrowsUnauthorized_Returns401()
    {
        var userId = Guid.NewGuid();
        _messageService.Setup(x => x.MarkAsReadAsync(userId))
            .ThrowsAsync(new UnauthorizedAccessException("Authentication is required."));

        var result = await _controller.MarkAsRead(userId);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_WhenUserIdEmpty_Returns400()
    {
        var result = await _controller.MarkAsRead(Guid.Empty);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }
}
