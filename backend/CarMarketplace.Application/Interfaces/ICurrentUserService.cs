namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user id.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user id from the execution context, or null when not authenticated.
    /// </summary>
    Guid? GetCurrentUserId();
}
