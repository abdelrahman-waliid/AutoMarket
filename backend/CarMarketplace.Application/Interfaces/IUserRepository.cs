using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Repository for user persistence. Implemented in Infrastructure.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by id (read-only, no tracking).
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by id for update/delete (tracked).
    /// </summary>
    Task<User?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email (read-only, no tracking).
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email for update operations (tracked).
    /// </summary>
    Task<User?> GetByEmailTrackedAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email for update operations (tracked), including soft-deleted users.
    /// </summary>
    Task<User?> GetByEmailIncludingDeletedTrackedAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users (read-only, no tracking).
    /// </summary>
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total users count.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users page ordered by newest first.
    /// </summary>
    Task<List<User>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user exists with the given email, optionally excluding a user id.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user exists with the given id.
    /// </summary>
    Task<bool> ExistsByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users by optional email and full name filter.
    /// </summary>
    Task<List<User>> SearchAsync(string? email = null, string? fullName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Add(User user);

    /// <summary>
    /// Marks a user for removal. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Remove(User user);

    /// <summary>
    /// Soft-deletes a user and related entities (cars/messages). Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    Task<bool> SoftDeleteCascadeAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
}
