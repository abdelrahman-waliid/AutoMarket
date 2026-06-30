using System.ComponentModel.DataAnnotations;

namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Query parameters for paginated car listing with filtering and sorting.
/// </summary>
public class CarQueryParametersDTO
{
    /// <summary>
    /// Gets or sets page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets optional title filter.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets optional brand filter.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Gets or sets optional exact year filter.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets free-text search (brand/model/title).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets optional minimum year filter.
    /// </summary>
    public int? MinYear { get; set; }

    /// <summary>
    /// Gets or sets optional maximum year filter.
    /// </summary>
    public int? MaxYear { get; set; }

    /// <summary>
    /// Gets or sets optional minimum price filter.
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Gets or sets optional maximum price filter.
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Gets or sets sort field (title, year, price, mileage, createdAt).
    /// </summary>
    [EnumDataType(typeof(CarSortBy))]
    public CarSortBy SortBy { get; set; } = CarSortBy.CreatedAt;

    /// <summary>
    /// Gets or sets sort direction (asc or desc).
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}
