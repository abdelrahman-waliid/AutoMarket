using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Services;

/// <summary>
/// Service implementation for car operations.
/// </summary>
public class CarService : ICarService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private const string DefaultSortOrder = "desc";

    private readonly ICarRepository _carRepository;
    private readonly IAIService _aiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CarService"/> class.
    /// </summary>
    /// <param name="carRepository">The car repository.</param>
    /// <param name="aiService">The AI service for price prediction.</param>
    /// <param name="unitOfWork">The unit of work for persisting changes.</param>
    public CarService(
        ICarRepository carRepository,
        IAIService aiService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _carRepository = carRepository;
        _aiService = aiService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<CarDTO>> GetCarsPagedAsync(CarQueryParametersDTO queryParameters)
    {
        var normalizedQuery = NormalizeQuery(queryParameters);
        return await GetCarsPagedCoreAsync(normalizedQuery, ownerId: null);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<CarDTO>> GetMyCarsPagedAsync(Guid ownerId, CarQueryParametersDTO queryParameters)
    {
        var normalizedQuery = NormalizeQuery(queryParameters);
        return await GetCarsPagedCoreAsync(normalizedQuery, ownerId);
    }

    /// <inheritdoc/>
    public async Task<CarResponseDTO?> GetCarByIdAsync(Guid carId)
    {
        var car = await _carRepository.GetByIdWithImagesTrackedAsync(carId);
        if (car == null)
        {
            return null;
        }

        car.Views++;
        car.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return MapToDTO<CarResponseDTO>(car);
    }

    /// <inheritdoc/>
    public async Task<CarDTO> AddCarAsync(CreateCarRequestDTO request)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var now = DateTime.UtcNow;
        var car = new Car
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Description = request.Description,
            Price = request.Price,
            Location = request.Location.Trim(),
            Status = "Active",
            Views = 0,
            Year = request.Year,
            Mileage = request.Mileage,
            FuelType = request.FuelType,
            TransmissionType = request.TransmissionType,
            OwnerId = currentUserId.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        _carRepository.Add(car);

        if (request.ImageUrls != null && request.ImageUrls.Any())
        {
            foreach (var imageUrl in request.ImageUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
            {
                _carRepository.AddCarImage(new CarImage
                {
                    Id = Guid.NewGuid(),
                    CarId = car.Id,
                    ImageUrl = imageUrl.Trim(),
                    CreatedAt = now
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var savedCar = await _carRepository.GetByIdWithImagesAsync(car.Id);
        return MapToDTO(savedCar!);
    }

    /// <inheritdoc/>
    public async Task<CarDTO?> UpdateCarAsync(CarDTO carDto)
    {
        var car = await _carRepository.GetByIdWithImagesTrackedAsync(carDto.Id);
        if (car == null)
        {
            return null;
        }

        car.Title = BuildTitle(carDto);
        car.Brand = ResolveBrand(carDto);
        car.Model = ResolveModel(carDto);
        car.Description = carDto.Description;
        car.Price = carDto.Price;
        car.Location = ResolveLocation(carDto);
        car.Status = string.IsNullOrWhiteSpace(carDto.Status) ? car.Status : carDto.Status.Trim();
        car.Year = carDto.Year;
        car.Mileage = carDto.Mileage;
        car.FuelType = carDto.FuelType;
        car.TransmissionType = carDto.TransmissionType;
        car.OwnerId = carDto.OwnerId;
        car.UpdatedAt = DateTime.UtcNow;

        if (carDto.ImageUrls != null)
        {
            _carRepository.RemoveCarImages(car.Images);

            foreach (var imageUrl in carDto.ImageUrls)
            {
                _carRepository.AddCarImage(new CarImage
                {
                    Id = Guid.NewGuid(),
                    CarId = car.Id,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var updatedCar = await _carRepository.GetByIdWithImagesAsync(car.Id);
        return MapToDTO(updatedCar!);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteCarAsync(Guid carId)
    {
        var car = await _carRepository.GetByIdTrackedAsync(carId);
        if (car == null)
        {
            return false;
        }

        _carRepository.Remove(car);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<List<CarDTO>> SearchCarsAsync(string? title, int? minYear, int? maxPrice)
    {
        var cars = await _carRepository.SearchAsync(title, minYear, maxPrice);
        return cars.Select(MapToDTO).ToList();
    }

    /// <inheritdoc/>
    public async Task<decimal> PredictCarPriceAsync(CarDTO carDto)
    {
        var estimateRequest = new PriceEstimateRequestDTO
        {
            Brand = ResolveBrand(carDto),
            Model = string.IsNullOrWhiteSpace(carDto.Model) ? carDto.Title.Trim() : carDto.Model.Trim(),
            Year = carDto.Year,
            Mileage = carDto.Mileage,
            FuelType = carDto.FuelType.ToString(),
            Transmission = carDto.TransmissionType.ToString(),
            Condition = "good",
            Location = carDto.Location,
            UserPrice = carDto.Price > 0 ? carDto.Price : null
        };

        var estimateResponse = await _aiService.EstimatePriceAsync(estimateRequest);
        return estimateResponse.EstimatedPrice;
    }

    private static string NormalizeSortOrder(string? sortOrder)
    {
        return string.Equals(sortOrder?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
            ? "asc"
            : DefaultSortOrder;
    }

    private async Task<PaginatedResult<CarDTO>> GetCarsPagedCoreAsync(CarQueryParametersDTO normalizedQuery, Guid? ownerId)
    {
        var (cars, totalCount) = await _carRepository.GetPagedWithImagesAsync(normalizedQuery, ownerId);

        return new PaginatedResult<CarDTO>
        {
            Items = cars.Select(MapToDTO).ToList(),
            TotalCount = totalCount,
            PageNumber = normalizedQuery.PageNumber,
            PageSize = normalizedQuery.PageSize
        };
    }

    private static CarQueryParametersDTO NormalizeQuery(CarQueryParametersDTO queryParameters)
    {
        return new CarQueryParametersDTO
        {
            PageNumber = queryParameters.PageNumber < 1 ? DefaultPageNumber : queryParameters.PageNumber,
            PageSize = queryParameters.PageSize < 1 ? DefaultPageSize : Math.Min(queryParameters.PageSize, MaxPageSize),
            Title = queryParameters.Title,
            Brand = queryParameters.Brand,
            Year = queryParameters.Year,
            Search = queryParameters.Search,
            MinYear = queryParameters.MinYear,
            MaxYear = queryParameters.MaxYear,
            MinPrice = queryParameters.MinPrice,
            MaxPrice = queryParameters.MaxPrice,
            SortBy = queryParameters.SortBy,
            SortOrder = NormalizeSortOrder(queryParameters.SortOrder)
        };
    }

    private static string ResolveBrand(CarDTO carDto)
    {
        if (!string.IsNullOrWhiteSpace(carDto.Brand))
        {
            return carDto.Brand.Trim();
        }

        return carDto.Title.Trim();
    }

    private static string ResolveModel(CarDTO carDto)
    {
        if (!string.IsNullOrWhiteSpace(carDto.Model))
        {
            return carDto.Model.Trim();
        }

        return carDto.Title.Trim();
    }

    private static string ResolveLocation(CarDTO carDto)
    {
        if (!string.IsNullOrWhiteSpace(carDto.Location))
        {
            return carDto.Location.Trim();
        }

        return "Unknown";
    }

    private static string BuildTitle(CarDTO carDto)
    {
        if (!string.IsNullOrWhiteSpace(carDto.Title))
        {
            return carDto.Title.Trim();
        }

        var brand = ResolveBrand(carDto);
        var model = ResolveModel(carDto);
        return $"{brand} {model}".Trim();
    }

    private static CarDTO MapToDTO(Car car)
    {
        return MapToDTO<CarDTO>(car);
    }

    private static T MapToDTO<T>(Car car) where T : CarDTO, new()
    {
        return new T
        {
            Id = car.Id,
            Title = car.Title,
            Brand = car.Brand,
            Model = car.Model,
            Description = car.Description,
            Price = car.Price,
            Location = car.Location,
            Status = car.Status,
            Views = car.Views,
            Year = car.Year,
            Mileage = car.Mileage,
            FuelType = car.FuelType,
            TransmissionType = car.TransmissionType,
            OwnerId = car.OwnerId,
            OwnerFullName = car.Owner?.FullName,
            OwnerEmail = car.Owner?.Email,
            OwnerAvatarUrl = car.Owner?.AvatarUrl,
            OwnerRole = car.Owner?.Role,
            OwnerCreatedAt = car.Owner?.CreatedAt,
            OwnerUpdatedAt = car.Owner?.UpdatedAt,
            CreatedAt = car.CreatedAt,
            UpdatedAt = car.UpdatedAt,
            ImageUrls = car.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>()
        };
    }
}
