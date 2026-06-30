using System.Security.Claims;
using CarMarketplace.API.DTOs;
using CarMarketplace.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Controller for managing car operations. Only authenticated users can create; only owner or Admin can update/delete.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;
    private readonly ICarImageUploadService _carImageUploadService;

    public CarsController(ICarService carService, ICarImageUploadService carImageUploadService)
    {
        _carService = carService;
        _carImageUploadService = carImageUploadService;
    }

    /// <summary>
    /// Gets the current user's id from JWT claims.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Returns true if the current user has the Admin role.
    /// </summary>
    private bool IsCurrentUserAdmin() => User.IsInRole("Admin");

    /// <summary>
    /// Retrieves cars from the system as a paginated list.
    /// </summary>
    /// <param name="queryParameters">
    /// Query options:
    /// pageNumber, pageSize, title, minYear, maxYear, minPrice, maxPrice, sortBy, sortOrder.
    /// </param>
    /// <returns>A paginated list of cars.</returns>
    /// <response code="200">Returns the paginated list of cars.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<CarDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<CarDTO>>> GetAllCars([FromQuery] CarQueryParametersDTO queryParameters)
    {
        var cars = await _carService.GetCarsPagedAsync(queryParameters);
        return Ok(cars);
    }

    /// <summary>
    /// Retrieves a car by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the car.</param>
    /// <returns>The car if found.</returns>
    /// <response code="200">Returns the requested car.</response>
    /// <response code="404">If the car is not found.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CarResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarResponseDTO>> GetCarById(Guid id)
    {
        var car = await _carService.GetCarByIdAsync(id);
        if (car == null)
        {
            return NotFound();
        }
        return Ok(car);
    }

    /// <summary>
    /// Creates a new car. Only authenticated users can create; the car is owned by the current user.
    /// </summary>
    /// <param name="request">Client payload for creating a car listing.</param>
    /// <returns>The created car with images.</returns>
    /// <response code="201">Returns the newly created car.</response>
    /// <response code="400">If the car data is invalid (FluentValidation).</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CarDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize]
    public async Task<ActionResult<CarDTO>> CreateCar([FromBody] CreateCarRequestDTO request)
    {
        var createdCar = await _carService.AddCarAsync(request);
        return CreatedAtAction(nameof(GetCarById), new { id = createdCar.Id }, createdCar);
    }

    /// <summary>
    /// Creates a new car with uploaded images. Images are validated and stored under wwwroot/uploads/cars.
    /// </summary>
    /// <param name="request">Multipart request containing car fields and image files.</param>
    /// <returns>The created car with stored image URLs.</returns>
    /// <response code="201">Returns the newly created car.</response>
    /// <response code="400">If image validation fails or request is invalid.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost("with-images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CarDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarDTO>> CreateCarWithImages([FromForm] CreateCarWithImagesRequestDTO request)
    {
        if (!_carImageUploadService.TryValidate(request.Images, out var validationError))
            return BadRequest(validationError);

        var imageUrls = await _carImageUploadService.SaveAsync(request.Images, Request);

        var createRequest = new CreateCarRequestDTO
        {
            Title = request.Title,
            Brand = request.Brand,
            Model = request.Model,
            Description = request.Description,
            Price = request.Price,
            Location = request.Location,
            Year = request.Year,
            Mileage = request.Mileage,
            FuelType = request.FuelType,
            TransmissionType = request.TransmissionType,
            ImageUrls = imageUrls
        };

        var createdCar = await _carService.AddCarAsync(createRequest);
        return CreatedAtAction(nameof(GetCarById), new { id = createdCar.Id }, createdCar);
    }

    /// <summary>
    /// Updates an existing car. Only the owner or Admin can update.
    /// </summary>
    /// <param name="id">The unique identifier of the car to update.</param>
    /// <param name="carDto">The updated car data (must match route id).</param>
    /// <returns>The updated car with images.</returns>
    /// <response code="200">Returns the updated car.</response>
    /// <response code="400">If the car data is invalid or ID mismatch.</response>
    /// <response code="403">If the user is not the owner and not Admin.</response>
    /// <response code="404">If the car is not found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CarDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarDTO>> UpdateCar(Guid id, [FromBody] CarDTO carDto)
    {
        if (id != carDto.Id)
            return BadRequest("Car ID in the URL does not match the ID in the request body.");

        var existing = await _carService.GetCarByIdAsync(id);
        if (existing == null)
            return NotFound();

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();
        if (existing.OwnerId != userId && !IsCurrentUserAdmin())
            return Forbid();

        var updatedCar = await _carService.UpdateCarAsync(carDto);
        return Ok(updatedCar!);
    }

    /// <summary>
    /// Deletes a car by its unique identifier. Only the owner or Admin can delete.
    /// </summary>
    /// <param name="id">The unique identifier of the car to delete.</param>
    /// <returns>No content if the car was deleted successfully.</returns>
    /// <response code="204">If the car was deleted successfully.</response>
    /// <response code="403">If the user is not the owner and not Admin.</response>
    /// <response code="404">If the car is not found.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCar(Guid id)
    {
        var existing = await _carService.GetCarByIdAsync(id);
        if (existing == null)
            return NotFound();

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();
        if (existing.OwnerId != userId && !IsCurrentUserAdmin())
            return Forbid();

        await _carService.DeleteCarAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Searches for cars based on optional criteria.
    /// </summary>
    /// <param name="title">Optional title filter for searching cars.</param>
    /// <param name="minYear">Optional minimum year filter for searching cars.</param>
    /// <param name="maxPrice">Optional maximum price filter for searching cars.</param>
    /// <returns>A list of cars matching the search criteria.</returns>
    /// <response code="200">Returns the list of matching cars.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<CarDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CarDTO>>> SearchCars(
        [FromQuery] string? title,
        [FromQuery] int? minYear,
        [FromQuery] int? maxPrice)
    {
        var cars = await _carService.SearchCarsAsync(title, minYear, maxPrice);
        return Ok(cars);
    }

    /// <summary>
    /// Predicts the price of a car based on its characteristics.
    /// </summary>
    /// <param name="carDto">The car data for price prediction.</param>
    /// <returns>The predicted price.</returns>
    /// <response code="200">Returns the predicted price.</response>
    /// <response code="400">If the car data is invalid.</response>
    [HttpPost("predict")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> PredictCarPrice([FromBody] CarDTO carDto)
    {
        var predictedPrice = await _carService.PredictCarPriceAsync(carDto);
        return Ok(predictedPrice);
    }
}
