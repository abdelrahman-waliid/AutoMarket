using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Admin-only car management endpoints.
/// </summary>
[ApiController]
[Route("api/admin/cars")]
[Authorize(Roles = "Admin")]
public class AdminCarsController : ControllerBase
{
    private readonly ICarService _carService;

    public AdminCarsController(ICarService carService)
    {
        _carService = carService;
    }

    /// <summary>
    /// Retrieves cars for administration purposes as a paginated list.
    /// </summary>
    /// <param name="queryParameters">Filtering, sorting, and pagination parameters.</param>
    /// <returns>A paginated list of cars including owner identifiers.</returns>
    /// <response code="200">Returns cars.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<CarDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<CarDTO>>> GetAllCarsForAdmin([FromQuery] CarQueryParametersDTO queryParameters)
    {
        var cars = await _carService.GetCarsPagedAsync(queryParameters);
        return Ok(cars);
    }
}
