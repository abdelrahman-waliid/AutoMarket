using Microsoft.EntityFrameworkCore;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Infrastructure.Data;

namespace CarMarketplace.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRefreshTokenRepository"/>.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public void Add(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
    }

    /// <inheritdoc/>
    public async Task<RefreshToken?> GetByTokenHashTrackedAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> RevokeByTokenHashAsync(string tokenHash, string reason, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
        if (entity == null)
            return false;
        entity.IsRevoked = true;
        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokedReason = reason;
        return true;
    }

    /// <inheritdoc/>
    public async Task<int> RevokeAllActiveForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
            token.RevokedReason = reason;
        }

        return activeTokens.Count;
    }
}
