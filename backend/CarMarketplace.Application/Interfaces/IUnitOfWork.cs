namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Unit of Work: coordinates persistence for one or more repositories. Call SaveChangesAsync once per use case to persist all pending changes. Implemented in Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes from the current context (repositories share the same context).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
