using System.Security.Claims;
using CarMarketplace.API.Controllers;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Controllers;

public class UsersControllerAuthorizationTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<CarMarketplace.API.Interfaces.IRefreshTokenCookieService> _refreshTokenCookieService = new();

    [Fact]
    public async Task UpdateUser_WhenCurrentUserIdMissing_Returns401()
    {
        var controller = CreateControllerWithClaims(userId: null, role: UserRole.User);
        var dto = BuildDto(Guid.NewGuid());

        var result = await controller.UpdateUser(dto.Id, dto);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenRoleMissing_Returns401()
    {
        var controller = CreateControllerWithClaims(Guid.NewGuid(), role: null);
        var dto = BuildDto(Guid.NewGuid());

        var result = await controller.UpdateUser(dto.Id, dto);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenNonAdminUpdatesAnotherUser_Returns403_AndDoesNotCallService()
    {
        var controller = CreateControllerWithClaims(Guid.NewGuid(), UserRole.User);
        var dto = BuildDto(Guid.NewGuid());

        var result = await controller.UpdateUser(dto.Id, dto);

        Assert.IsType<ForbidResult>(result.Result);
        _userService.Verify(
            x => x.UpdateUserAsync(It.IsAny<UserDTO>(), It.IsAny<UserRole>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WhenAdminUpdatesAnotherUser_CallsServiceAndReturns200()
    {
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var controller = CreateControllerWithClaims(adminId, UserRole.Admin);
        var dto = BuildDto(targetUserId);

        _userService.Setup(x => x.UpdateUserAsync(dto, UserRole.Admin, adminId))
            .ReturnsAsync(dto);

        var result = await controller.UpdateUser(targetUserId, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
        _userService.Verify(x => x.UpdateUserAsync(dto, UserRole.Admin, adminId), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenServiceThrowsForbidden_Returns403()
    {
        var userId = Guid.NewGuid();
        var controller = CreateControllerWithClaims(userId, UserRole.User);
        var dto = BuildDto(userId);

        _userService.Setup(x => x.UpdateUserAsync(dto, UserRole.User, userId))
            .ThrowsAsync(new ForbiddenAccessException());

        var result = await controller.UpdateUser(userId, dto);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private UsersController CreateControllerWithClaims(Guid? userId, UserRole? role)
    {
        var controller = new UsersController(_userService.Object, _refreshTokenCookieService.Object);

        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (role.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));
        }

        var identity = claims.Any()
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    private static UserDTO BuildDto(Guid id)
    {
        return new UserDTO
        {
            Id = id,
            FullName = "Test User",
            Email = "test@example.com",
            Role = UserRole.User
        };
    }
}
