using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Admin-only listing management endpoints.
/// </summary>
[ApiController]
[Route("api/admin/listings")]
[Authorize(Roles = "Admin")]
public class AdminListingsController : ControllerBase
{
    private readonly ICarService _carService;

    public AdminListingsController(ICarService carService)
    {
        _carService = carService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<CarDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<CarDTO>>> GetListings([FromQuery] CarQueryParametersDTO queryParameters)
    {
        var listings = await _carService.GetCarsPagedAsync(queryParameters);
        return Ok(listings);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteListing(Guid id)
    {
        var deleted = await _carService.DeleteCarAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
