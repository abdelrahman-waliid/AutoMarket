using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Current user's car listings.
/// </summary>
[ApiController]
[Route("api/my-cars")]
[Authorize]
public class MyCarsController : ControllerBase
{
    private readonly ICarService _carService;
    private readonly ICurrentUserService _currentUserService;

    public MyCarsController(ICarService carService, ICurrentUserService currentUserService)
    {
        _carService = carService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Returns current user's listings as a paginated result.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<CarDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<CarDTO>>> GetMyCars([FromQuery] CarQueryParametersDTO queryParameters)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        var result = await _carService.GetMyCarsPagedAsync(currentUserId.Value, queryParameters);
        return Ok(result);
    }
}
