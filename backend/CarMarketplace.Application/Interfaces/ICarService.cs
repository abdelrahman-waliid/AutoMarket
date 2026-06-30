using CarMarketplace.Application.DTOs;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Represents the business logic contract for car operations.
/// </summary>
public interface ICarService
{
    /// <summary>
    /// Retrieves cars from the system as a paginated result asynchronously.
    /// </summary>
    /// <param name="queryParameters">Filtering, sorting, and pagination parameters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains paged car DTOs and metadata.</returns>
    Task<PaginatedResult<CarDTO>> GetCarsPagedAsync(CarQueryParametersDTO queryParameters);

    /// <summary>
    /// Retrieves a paginated list of cars owned by a specific user.
    /// </summary>
    Task<PaginatedResult<CarDTO>> GetMyCarsPagedAsync(Guid ownerId, CarQueryParametersDTO queryParameters);

    /// <summary>
    /// Retrieves a car by its unique identifier asynchronously.
    /// </summary>
    /// <param name="carId">The unique identifier of the car to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the car DTO if found; otherwise, null.</returns>
    Task<CarResponseDTO?> GetCarByIdAsync(Guid carId);

    /// <summary>
    /// Adds a new car to the system asynchronously.
    /// </summary>
    /// <param name="request">The car creation payload from client.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created car DTO.</returns>
    Task<CarDTO> AddCarAsync(CreateCarRequestDTO request);

    /// <summary>
    /// Updates an existing car in the system asynchronously.
    /// </summary>
    /// <param name="carDto">The car DTO containing the updated car information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated car DTO if found; otherwise, null.</returns>
    Task<CarDTO?> UpdateCarAsync(CarDTO carDto);

    /// <summary>
    /// Deletes a car from the system asynchronously.
    /// </summary>
    /// <param name="carId">The unique identifier of the car to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the car was successfully deleted.</returns>
    Task<bool> DeleteCarAsync(Guid carId);

    /// <summary>
    /// Searches for cars based on specified criteria asynchronously.
    /// </summary>
    /// <param name="title">Optional title filter for searching cars.</param>
    /// <param name="minYear">Optional minimum year filter for searching cars.</param>
    /// <param name="maxPrice">Optional maximum price filter for searching cars.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of car DTOs matching the search criteria.</returns>
    Task<List<CarDTO>> SearchCarsAsync(string? title, int? minYear, int? maxPrice);

    /// <summary>
    /// Predicts the price of a car based on its characteristics asynchronously.
    /// </summary>
    /// <param name="carDto">The car DTO containing the car information for price prediction.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the predicted price.</returns>
    Task<decimal> PredictCarPriceAsync(CarDTO carDto);
}
