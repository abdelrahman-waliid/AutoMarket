using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.Application.Services;

/// <summary>
/// Dashboard aggregation service for current user.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ICarRepository _carRepository;
    private readonly IMessageRepository _messageRepository;

    public DashboardService(ICarRepository carRepository, IMessageRepository messageRepository)
    {
        _carRepository = carRepository;
        _messageRepository = messageRepository;
    }

    public async Task<DashboardDTO> GetDashboardAsync(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var totalCarsOwned = await _carRepository.CountByOwnerAsync(currentUserId);
        var totalViewsAcrossListings = await _carRepository.SumViewsByOwnerAsync(currentUserId);
        var unreadMessagesCount = await _messageRepository.CountUnreadForReceiverAsync(currentUserId);

        var recentCars = await _carRepository.GetRecentByOwnerAsync(currentUserId, 5);
        var recentMessages = await _messageRepository.GetRecentForUserAsync(currentUserId, 5);

        var recentActivity = recentCars
            .Select(car => new DashboardActivityDTO
            {
                Type = "CarListing",
                Description = $"Listing '{car.Title}' created.",
                OccurredAt = car.CreatedAt
            })
            .Concat(
                recentMessages.Select(message => new DashboardActivityDTO
                {
                    Type = "Message",
                    Description = message.SenderId == currentUserId
                        ? "You sent a message."
                        : "You received a message.",
                    OccurredAt = message.CreatedAt
                }))
            .OrderByDescending(item => item.OccurredAt)
            .Take(5)
            .ToList();

        return new DashboardDTO
        {
            TotalCarsOwned = totalCarsOwned,
            UnreadMessagesCount = unreadMessagesCount,
            TotalViewsAcrossListings = totalViewsAcrossListings,
            RecentActivity = recentActivity
        };
    }
}
