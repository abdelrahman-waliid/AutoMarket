namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Generic paginated response payload.
/// </summary>
/// <typeparam name="T">Type of each item in the page.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Gets or sets current page items.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// Gets or sets total number of records for the query.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets total number of pages.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
