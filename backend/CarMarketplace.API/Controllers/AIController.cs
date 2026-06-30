using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Controller for AI-powered operations including price prediction.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIController"/> class.
    /// </summary>
    /// <param name="aiService">The AI service.</param>
    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Returns AI placeholder price estimate range for marketplace usage.
    /// </summary>
    [HttpPost("price-estimate")]
    [ProducesResponseType(typeof(PriceEstimateResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceEstimateResponseDTO>> EstimatePrice([FromBody] PriceEstimateRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand) || string.IsNullOrWhiteSpace(request.Model))
        {
            return BadRequest("Brand and model are required.");
        }

        if (request.Year < 1900 || request.Year > DateTime.UtcNow.Year)
        {
            return BadRequest($"Year must be between 1900 and {DateTime.UtcNow.Year}.");
        }

        if (request.Mileage < 0)
        {
            return BadRequest("Mileage must be greater than or equal to zero.");
        }

        var response = await _aiService.EstimatePriceAsync(request);
        return Ok(response);
    }

}
