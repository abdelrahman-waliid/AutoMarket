using Microsoft.EntityFrameworkCore;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Infrastructure.Data;

namespace CarMarketplace.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICarRepository"/> using <see cref="AppDbContext"/>.
/// </summary>
public class CarRepository : ICarRepository
{
    private readonly AppDbContext _context;

    public CarRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<Car>> GetAllWithImagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(List<Car> Items, int TotalCount)> GetPagedWithImagesAsync(
        CarQueryParametersDTO queryParameters,
        Guid? ownerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Cars.AsNoTracking().AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(c => c.OwnerId == ownerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Title))
            query = query.Where(c => c.Title.Contains(queryParameters.Title));
        if (!string.IsNullOrWhiteSpace(queryParameters.Brand))
            query = query.Where(c => c.Brand.Contains(queryParameters.Brand));
        if (queryParameters.Year.HasValue)
            query = query.Where(c => c.Year == queryParameters.Year.Value);
        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            var search = queryParameters.Search.Trim();
            query = query.Where(c => c.Brand.Contains(search) || c.Model.Contains(search) || c.Title.Contains(search));
        }
        if (queryParameters.MinYear.HasValue)
            query = query.Where(c => c.Year >= queryParameters.MinYear.Value);
        if (queryParameters.MaxYear.HasValue)
            query = query.Where(c => c.Year <= queryParameters.MaxYear.Value);
        if (queryParameters.MinPrice.HasValue)
            query = query.Where(c => c.Price >= queryParameters.MinPrice.Value);
        if (queryParameters.MaxPrice.HasValue)
            query = query.Where(c => c.Price <= queryParameters.MaxPrice.Value);

        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortOrder);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(c => c.Owner)
            .Include(c => c.Images)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Car?> GetByIdWithImagesAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == carId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Car?> GetByIdWithImagesTrackedAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .Include(c => c.Owner)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == carId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Car?> GetByIdTrackedAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .FirstOrDefaultAsync(c => c.Id == carId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Car>> SearchAsync(string? title = null, int? minYear = null, int? maxPrice = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Cars
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(c => c.Title.Contains(title) || c.Brand.Contains(title) || c.Model.Contains(title));
        if (minYear.HasValue)
            query = query.Where(c => c.Year >= minYear.Value);
        if (maxPrice.HasValue)
            query = query.Where(c => c.Price <= maxPrice.Value);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(Car car)
    {
        _context.Cars.Add(car);
    }

    /// <inheritdoc/>
    public void AddCarImage(CarImage carImage)
    {
        _context.CarImages.Add(carImage);
    }

    /// <inheritdoc/>
    public void RemoveCarImages(IEnumerable<CarImage> images)
    {
        _context.CarImages.RemoveRange(images);
    }

    /// <inheritdoc/>
    public void Remove(Car car)
    {
        _context.Cars.Remove(car);
    }

    /// <inheritdoc/>
    public async Task<int> CountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .AsNoTracking()
            .CountAsync(c => c.OwnerId == ownerId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> SumViewsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        // SUM() returns NULL when there are no rows; cast to nullable and coalesce to 0.
        var sum = await _context.Cars
            .AsNoTracking()
            .Where(c => c.OwnerId == ownerId)
            .SumAsync(c => (int?)c.Views, cancellationToken);

        return sum ?? 0;
    }

    /// <inheritdoc/>
    public async Task<List<Car>> GetRecentByOwnerAsync(Guid ownerId, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Cars
            .AsNoTracking()
            .Where(c => c.OwnerId == ownerId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Car> ApplySorting(IQueryable<Car> query, CarSortBy sortBy, string? sortOrder)
    {
        var isAscending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isAscending) switch
        {
            (CarSortBy.Title, true) => query.OrderBy(c => c.Title).ThenBy(c => c.Id),
            (CarSortBy.Title, false) => query.OrderByDescending(c => c.Title).ThenByDescending(c => c.Id),
            (CarSortBy.Year, true) => query.OrderBy(c => c.Year).ThenBy(c => c.Id),
            (CarSortBy.Year, false) => query.OrderByDescending(c => c.Year).ThenByDescending(c => c.Id),
            (CarSortBy.Price, true) => query.OrderBy(c => c.Price).ThenBy(c => c.Id),
            (CarSortBy.Price, false) => query.OrderByDescending(c => c.Price).ThenByDescending(c => c.Id),
            (CarSortBy.Mileage, true) => query.OrderBy(c => c.Mileage).ThenBy(c => c.Id),
            (CarSortBy.Mileage, false) => query.OrderByDescending(c => c.Mileage).ThenByDescending(c => c.Id),
            (CarSortBy.CreatedAt, true) => query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
            _ => query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
        };
    }
}
