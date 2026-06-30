using System.Collections.Concurrent;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.API.Services;

/// <summary>
/// In-memory per-user SignalR connection tracker. Suitable for a single API instance.
/// </summary>
public sealed class InMemoryUserConnectionTracker : IUserConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, UserConnections> _connections = new();

    public bool AddConnection(Guid userId, string connectionId)
    {
        while (true)
        {
            var entry = _connections.GetOrAdd(userId, _ => new UserConnections());

            lock (entry.Gate)
            {
                if (entry.Removed)
                {
                    continue;
                }

                var wasOffline = entry.ConnectionIds.Count == 0;
                entry.ConnectionIds.Add(connectionId);
                return wasOffline;
            }
        }
    }

    public bool RemoveConnection(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var entry))
        {
            return false;
        }

        lock (entry.Gate)
        {
            entry.ConnectionIds.Remove(connectionId);
            if (entry.ConnectionIds.Count > 0)
            {
                return false;
            }

            entry.Removed = true;
            _connections.TryRemove(userId, out _);
            return true;
        }
    }

    public bool IsOnline(Guid userId)
    {
        if (!_connections.TryGetValue(userId, out var entry))
        {
            return false;
        }

        lock (entry.Gate)
        {
            return !entry.Removed && entry.ConnectionIds.Count > 0;
        }
    }

    public IReadOnlyCollection<string> GetConnectionIds(Guid userId)
    {
        if (!_connections.TryGetValue(userId, out var entry))
        {
            return [];
        }

        lock (entry.Gate)
        {
            return entry.ConnectionIds.ToArray();
        }
    }

    private sealed class UserConnections
    {
        public object Gate { get; } = new();

        public HashSet<string> ConnectionIds { get; } = [];

        public bool Removed { get; set; }
    }
}
