using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Repository for refresh token persistence. Implemented in Infrastructure.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Adds a refresh token. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    void Add(RefreshToken refreshToken);

    /// <summary>
    /// Gets a refresh token by token hash for validation/rotation (tracked).
    /// </summary>
    Task<RefreshToken?> GetByTokenHashTrackedAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a refresh token by token hash (tracked) and marks it revoked. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
    /// </summary>
    Task<bool> RevokeByTokenHashAsync(string tokenHash, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for a user. Returns the number of tokens updated.
    /// </summary>
    Task<int> RevokeAllActiveForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}
