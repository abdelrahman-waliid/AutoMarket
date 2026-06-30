using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Controller for managing message operations between users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagesController"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    /// <summary>
    /// Retrieves conversation summaries for the current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ConversationDTO>>> GetConversations()
    {
        try
        {
            var conversations = await _messageService.GetConversationsAsync();
            return Ok(conversations);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves all messages for a specific user (both sent and received).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of messages associated with the user.</returns>
    /// <response code="200">Returns the list of messages for the user.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If trying to access another user's messages.</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<MessageDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MessageDTO>>> GetMessagesForUser(Guid userId)
    {
        try
        {
            var messages = await _messageService.GetMessagesForUserAsync(userId);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ForbiddenAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Retrieves all messages for a specific user (route alias).
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(List<MessageDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<ActionResult<List<MessageDTO>>> GetMessagesForUserAlias(Guid userId)
    {
        return GetMessagesForUser(userId);
    }

    /// <summary>
    /// Retrieves all messages exchanged between two specific users.
    /// </summary>
    /// <param name="user1Id">The unique identifier of the first user.</param>
    /// <param name="user2Id">The unique identifier of the second user.</param>
    /// <returns>A list of messages exchanged between the two users.</returns>
    /// <response code="200">Returns the list of messages between the two users.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If trying to access a conversation not involving the current user.</response>
    [HttpGet("between/{user1Id}/{user2Id}")]
    [ProducesResponseType(typeof(List<MessageDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<MessageDTO>>> GetMessagesBetweenUsers(Guid user1Id, Guid user2Id)
    {
        try
        {
            var messages = await _messageService.GetMessagesBetweenUsersAsync(user1Id, user2Id);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ForbiddenAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Sends a new message between users.
    /// </summary>
    /// <param name="messageDto">The message data to send.</param>
    /// <returns>The created message.</returns>
    /// <response code="201">Returns the newly created message.</response>
    /// <response code="400">If the message data is invalid or sender/receiver does not exist.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageDTO>> SendMessage([FromBody] MessageDTO messageDto)
    {
        try
        {
            var createdMessage = await _messageService.SendMessageAsync(messageDto);
            return CreatedAtAction(nameof(GetMessagesBetweenUsers), 
                new { user1Id = createdMessage.SenderId, user2Id = createdMessage.ReceiverId }, 
                createdMessage);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a message by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the message to delete.</param>
    /// <returns>No content if the message was deleted successfully.</returns>
    /// <response code="204">If the message was deleted successfully.</response>
    /// <response code="404">If the message is not found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If trying to delete a message not involving the current user.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("Message ID is required.");
        }

        try
        {
            var deleted = await _messageService.DeleteMessageAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ForbiddenAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Marks all unread messages from a specific sender as read for the current user.
    /// </summary>
    /// <param name="userId">The sender user identifier.</param>
    [HttpPost("mark-as-read/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest("User ID is required.");
        }

        try
        {
            await _messageService.MarkAsReadAsync(userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
