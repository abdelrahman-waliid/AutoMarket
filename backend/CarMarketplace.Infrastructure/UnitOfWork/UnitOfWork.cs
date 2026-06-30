using CarMarketplace.Application.Interfaces;
using CarMarketplace.Infrastructure.Data;

namespace CarMarketplace.Infrastructure.UnitOfWork;

/// <summary>
/// Unit of Work implementation using <see cref="AppDbContext"/>. Repositories and this UoW share the same context (scoped), so one SaveChangesAsync persists all pending changes.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
