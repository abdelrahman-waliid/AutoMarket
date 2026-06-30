namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Represents a conversation summary for the current user.
/// </summary>
public class ConversationDTO
{
    public Guid OtherUserId { get; set; }

    public string OtherUserName { get; set; } = string.Empty;

    public string? OtherUserAvatar { get; set; }

    public string LastMessage { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; }

    public int UnreadCount { get; set; }

    public bool IsOnline { get; set; }

    public DateTime? LastSeen { get; set; }
}
