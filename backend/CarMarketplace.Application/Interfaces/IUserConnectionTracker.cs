namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Tracks active realtime connections per user for presence and delivery decisions.
/// </summary>
public interface IUserConnectionTracker
{
    bool AddConnection(Guid userId, string connectionId);

    bool RemoveConnection(Guid userId, string connectionId);

    bool IsOnline(Guid userId);

    IReadOnlyCollection<string> GetConnectionIds(Guid userId);
}
