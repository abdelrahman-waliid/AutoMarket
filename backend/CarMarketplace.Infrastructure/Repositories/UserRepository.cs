using Microsoft.EntityFrameworkCore;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Infrastructure.Data;

namespace CarMarketplace.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/> using <see cref="AppDbContext"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailTrackedAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailIncludingDeletedTrackedAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Email)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().Where(u => u.Email == email);
        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<User>> SearchAsync(string? email = null, string? fullName = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(u => u.Email.Contains(email));
        if (!string.IsNullOrWhiteSpace(fullName))
            query = query.Where(u => u.FullName.Contains(fullName));

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    /// <inheritdoc/>
    public void Remove(User user)
    {
        _context.Users.Remove(user);
    }

    /// <inheritdoc/>
    public async Task<bool> SoftDeleteCascadeAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return false;
        }

        if (!user.IsDeleted)
        {
            user.IsDeleted = true;
            user.UpdatedAt = utcNow;
        }

        var cars = await _context.Cars
            .IgnoreQueryFilters()
            .Where(c => c.OwnerId == userId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var car in cars)
        {
            car.IsDeleted = true;
            car.UpdatedAt = utcNow;
        }

        var messages = await _context.Messages
            .IgnoreQueryFilters()
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.IsDeleted = true;
        }

        return true;
    }
}
