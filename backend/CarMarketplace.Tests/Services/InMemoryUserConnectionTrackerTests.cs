using CarMarketplace.API.Services;
using Xunit;

namespace CarMarketplace.Tests.Services;

public class InMemoryUserConnectionTrackerTests
{
    [Fact]
    public void AddAndRemoveConnection_TracksMultipleDevicesWithoutDuplicateOnlineOfflineTransitions()
    {
        var tracker = new InMemoryUserConnectionTracker();
        var userId = Guid.NewGuid();
        var firstConnection = Guid.NewGuid().ToString("N");
        var secondConnection = Guid.NewGuid().ToString("N");

        var firstBecameOnline = tracker.AddConnection(userId, firstConnection);
        var secondBecameOnline = tracker.AddConnection(userId, secondConnection);
        var firstBecameOffline = tracker.RemoveConnection(userId, firstConnection);
        var secondBecameOffline = tracker.RemoveConnection(userId, secondConnection);

        Assert.True(firstBecameOnline);
        Assert.False(secondBecameOnline);
        Assert.False(firstBecameOffline);
        Assert.True(secondBecameOffline);
        Assert.False(tracker.IsOnline(userId));
        Assert.Empty(tracker.GetConnectionIds(userId));
    }
}
