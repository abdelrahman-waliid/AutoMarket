using CarMarketplace.Application.DTOs;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Represents the business logic contract for user messaging operations.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Retrieves current user's conversation summaries.
    /// </summary>
    Task<List<ConversationDTO>> GetConversationsAsync();

    /// <summary>
    /// Retrieves all messages exchanged between two specific users asynchronously.
    /// </summary>
    /// <param name="user1Id">The unique identifier of the first user.</param>
    /// <param name="user2Id">The unique identifier of the second user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of message DTOs exchanged between the two users.</returns>
    Task<List<MessageDTO>> GetMessagesBetweenUsersAsync(Guid user1Id, Guid user2Id);

    /// <summary>
    /// Retrieves all messages for a specific user (both sent and received) asynchronously.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of message DTOs associated with the user.</returns>
    Task<List<MessageDTO>> GetMessagesForUserAsync(Guid userId);

    /// <summary>
    /// Sends a new message asynchronously.
    /// </summary>
    /// <param name="messageDto">The message DTO containing the message information to send.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created message DTO.</returns>
    Task<MessageDTO> SendMessageAsync(MessageDTO messageDto);

    /// <summary>
    /// Deletes a message from the system asynchronously.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the message was successfully deleted.</returns>
    Task<bool> DeleteMessageAsync(Guid messageId);

    /// <summary>
    /// Marks all unread messages from a specific sender as read for the current user.
    /// </summary>
    /// <param name="userId">The sender identifier.</param>
    /// <returns>The number of updated messages.</returns>
    Task<int> MarkAsReadAsync(Guid userId);

    /// <summary>
    /// Marks pending incoming messages as delivered when the receiver comes online.
    /// </summary>
    Task<int> MarkPendingMessagesDeliveredAsync(Guid receiverId);
}
