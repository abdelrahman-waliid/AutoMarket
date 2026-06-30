using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// User dashboard endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Returns dashboard aggregates for current authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardDTO>> GetDashboard()
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var dashboard = await _dashboardService.GetDashboardAsync(currentUserId.Value);
        return Ok(dashboard);
    }
}
