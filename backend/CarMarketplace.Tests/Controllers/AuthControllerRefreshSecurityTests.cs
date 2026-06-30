using CarMarketplace.API.Controllers;
using CarMarketplace.API.Interfaces;
using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Exceptions;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Controllers;

public class AuthControllerRefreshSecurityTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IRefreshTokenCookieService> _cookieService = new();
    private readonly Mock<IRefreshRequestCsrfProtectionService> _csrfProtectionService = new();
    private readonly Mock<IPasswordResetEmailService> _passwordResetEmailService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    [Fact]
    public async Task Refresh_WhenCookieMissing_Returns401()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(true);
        var missing = string.Empty;
        _cookieService.Setup(x => x.TryGetFromRequest(It.IsAny<HttpRequest>(), out missing))
            .Returns(false);

        var result = await controller.Refresh();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenTokenInvalid_Returns401AndClearsCookie()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(true);
        var refreshToken = "refresh-token";
        _cookieService.Setup(x => x.TryGetFromRequest(It.IsAny<HttpRequest>(), out refreshToken))
            .Returns(true);
        _userService.Setup(x => x.RefreshAccessTokenAsync(refreshToken))
            .ReturnsAsync((AuthSessionDTO?)null);

        var result = await controller.Refresh();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(401, unauthorized.StatusCode);
        _cookieService.Verify(x => x.ClearCookie(It.IsAny<HttpResponse>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenReuseDetected_Returns403AndClearsCookie()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(true);
        var refreshToken = "refresh-token";
        _cookieService.Setup(x => x.TryGetFromRequest(It.IsAny<HttpRequest>(), out refreshToken))
            .Returns(true);
        _userService.Setup(x => x.RefreshAccessTokenAsync(refreshToken))
            .ThrowsAsync(new RefreshTokenReuseDetectedException("Reuse detected."));

        var result = await controller.Refresh();

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbidden.StatusCode);
        _cookieService.Verify(x => x.ClearCookie(It.IsAny<HttpResponse>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenSuccessful_SetsNewCookieAndReturnsToken()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(true);
        var refreshToken = "refresh-token";
        var session = new AuthSessionDTO
        {
            Response = new LoginResponseDTO
            {
                User = new UserDTO
                {
                    Id = Guid.NewGuid(),
                    FullName = "User",
                    Email = "user@test.com",
                    Role = UserRole.User
                },
                Token = "new-access-token"
            },
            RefreshToken = "new-refresh-token"
        };

        _cookieService.Setup(x => x.TryGetFromRequest(It.IsAny<HttpRequest>(), out refreshToken))
            .Returns(true);
        _userService.Setup(x => x.RefreshAccessTokenAsync(refreshToken))
            .ReturnsAsync(session);

        var result = await controller.Refresh();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponseDTO>(ok.Value);
        Assert.Equal("new-access-token", payload.Token);
        _cookieService.Verify(x => x.SetCookie(It.IsAny<HttpResponse>(), "new-refresh-token"), Times.Once);
    }

    [Fact]
    public async Task Logout_WhenCookiePresent_RevokesAndClearsCookie()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(true);
        var refreshToken = "refresh-token";
        _cookieService.Setup(x => x.TryGetFromRequest(It.IsAny<HttpRequest>(), out refreshToken))
            .Returns(true);
        _userService.Setup(x => x.RevokeRefreshTokenAsync(refreshToken)).ReturnsAsync(true);

        var result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        _userService.Verify(x => x.RevokeRefreshTokenAsync(refreshToken), Times.Once);
        _cookieService.Verify(x => x.ClearCookie(It.IsAny<HttpResponse>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenCsrfValidationFails_Returns403()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(false);

        var result = await controller.Refresh();

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Logout_WhenCsrfValidationFails_Returns403()
    {
        var controller = CreateController();
        _csrfProtectionService.Setup(x => x.IsRequestAllowed(It.IsAny<HttpRequest>())).Returns(false);

        var result = await controller.Logout();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WhenSuccessful_Returns200AndClearsRefreshCookie()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController();
        var request = new ChangePasswordRequestDTO
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(userId);
        _userService.Setup(x => x.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(true);

        var result = await controller.ChangePassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        _cookieService.Verify(x => x.ClearCookie(It.IsAny<HttpResponse>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WhenCurrentPasswordWrong_Returns400()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController();
        var request = new ChangePasswordRequestDTO
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!"
        };

        _currentUserService.Setup(x => x.GetCurrentUserId()).Returns(userId);
        _userService.Setup(x => x.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword))
            .ReturnsAsync(false);

        var result = await controller.ChangePassword(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        _cookieService.Verify(x => x.ClearCookie(It.IsAny<HttpResponse>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_WhenTokenGenerated_ReturnsGenericResponseAndSendsEmail()
    {
        var controller = CreateController();
        var request = new ForgotPasswordRequestDTO { Email = "reset@example.com" };
        _userService.Setup(x => x.ForgotPasswordAsync(request.Email))
            .ReturnsAsync("sensitive-reset-token");

        var result = await controller.ForgotPassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("If an account with that email exists, password reset instructions were sent.", payloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("resetToken", payloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive-reset-token", payloadJson, StringComparison.Ordinal);
        _passwordResetEmailService.Verify(
            x => x.SendPasswordResetAsync(
                request.Email,
                "sensitive-reset-token",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WhenUserMissing_ReturnsGenericResponseAndDoesNotSendEmail()
    {
        var controller = CreateController();
        var request = new ForgotPasswordRequestDTO { Email = "missing@example.com" };
        _userService.Setup(x => x.ForgotPasswordAsync(request.Email))
            .ReturnsAsync((string?)null);

        var result = await controller.ForgotPassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("If an account with that email exists, password reset instructions were sent.", payloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("resetToken", payloadJson, StringComparison.OrdinalIgnoreCase);
        _passwordResetEmailService.Verify(
            x => x.SendPasswordResetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailSendingFails_Throws()
    {
        var controller = CreateController();
        var request = new ForgotPasswordRequestDTO { Email = "reset@example.com" };
        _userService.Setup(x => x.ForgotPasswordAsync(request.Email))
            .ReturnsAsync("sensitive-reset-token");
        _passwordResetEmailService.Setup(x => x.SendPasswordResetAsync(
                request.Email,
                "sensitive-reset-token",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ForgotPassword(request));
    }

    private AuthController CreateController()
    {
        var controller = new AuthController(
            _userService.Object,
            _cookieService.Object,
            _csrfProtectionService.Object,
            _passwordResetEmailService.Object,
            _currentUserService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }
}
