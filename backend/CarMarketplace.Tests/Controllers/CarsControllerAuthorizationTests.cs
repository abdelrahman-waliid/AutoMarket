using System.Security.Claims;
using CarMarketplace.API.Controllers;
using CarMarketplace.API.Interfaces;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Controllers;

public class CarsControllerAuthorizationTests
{
    private readonly Mock<ICarService> _carService = new();
    private readonly Mock<ICarImageUploadService> _imageUploadService = new();

    [Fact]
    public async Task UpdateCar_WhenUserIsNotOwnerAndNotAdmin_ReturnsForbid()
    {
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _carService.Setup(x => x.GetCarByIdAsync(carId)).ReturnsAsync(new CarResponseDTO
        {
            Id = carId,
            OwnerId = ownerId
        });

        var controller = CreateController(currentUserId, UserRole.User.ToString());

        var result = await controller.UpdateCar(carId, new CarDTO
        {
            Id = carId,
            OwnerId = ownerId
        });

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task DeleteCar_WhenUserIsAdmin_AllowsDelete()
    {
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _carService.Setup(x => x.GetCarByIdAsync(carId)).ReturnsAsync(new CarResponseDTO
        {
            Id = carId,
            OwnerId = ownerId
        });
        _carService.Setup(x => x.DeleteCarAsync(carId)).ReturnsAsync(true);

        var controller = CreateController(currentUserId, UserRole.Admin.ToString());

        var result = await controller.DeleteCar(carId);

        Assert.IsType<NoContentResult>(result);
        _carService.Verify(x => x.DeleteCarAsync(carId), Times.Once);
    }

    private CarsController CreateController(Guid userId, string role)
    {
        var controller = new CarsController(_carService.Object, _imageUploadService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "TestAuth"))
            }
        };

        return controller;
    }
}
