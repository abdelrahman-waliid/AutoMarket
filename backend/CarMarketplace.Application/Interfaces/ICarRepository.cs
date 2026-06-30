using CarMarketplace.Application.DTOs;
using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Repository for car and car image persistence. Implemented in Infrastructure.
/// </summary>
public interface ICarRepository
{
    /// <summary>
    /// Gets all cars with images (read-only, no tracking).
    /// </summary>
    Task<List<Car>> GetAllWithImagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single page of cars with images and the total count.
    /// </summary>
    /// <param name="queryParameters">Filtering, sorting, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged items and total record count.</returns>
    Task<(List<Car> Items, int TotalCount)> GetPagedWithImagesAsync(
        CarQueryParametersDTO queryParameters,
        Guid? ownerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a car by id with images (read-only, no tracking).
    /// </summary>
    Task<Car?> GetByIdWithImagesAsync(Guid carId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a car by id with images (tracked, for update).
    /// </summary>
    Task<Car?> GetByIdWithImagesTrackedAsync(Guid carId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a car by id (tracked, for delete).
    /// </summary>
    Task<Car?> GetByIdTrackedAsync(Guid carId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts cars for a specific owner.
    /// </summary>
    Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums listing views for a specific owner.
    /// </summary>
    Task<int> SumViewsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latest cars created by a specific owner.
    /// </summary>
    Task<List<Car>> GetRecentByOwnerAsync(Guid ownerId, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches cars with optional filters (title, min year, max price). Includes images.
    /// </summary>
    Task<List<Car>> SearchAsync(string? title = null, int? minYear = null, int? maxPrice = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a car. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Add(Car car);

    /// <summary>
    /// Adds a car image. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void AddCarImage(CarImage carImage);

    /// <summary>
    /// Removes a range of car images. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void RemoveCarImages(IEnumerable<CarImage> images);

    /// <summary>
    /// Marks a car for removal. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Remove(Car car);
}
