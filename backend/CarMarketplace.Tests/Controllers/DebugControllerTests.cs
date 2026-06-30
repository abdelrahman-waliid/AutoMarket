using CarMarketplace.API.Configuration;
using CarMarketplace.API.Controllers;
using CarMarketplace.API.Interfaces;
using CarMarketplace.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CarMarketplace.Tests.Controllers;

public class DebugControllerTests
{
    private readonly Mock<IPasswordResetEmailService> _passwordResetEmailService = new();

    [Fact]
    public async Task TestEmail_WhenNotDevelopment_ReturnsNotFound()
    {
        var controller = CreateController(
            Environments.Production,
            new PasswordResetEmailSettings { TestRecipientEmail = "dev@example.com" });

        var result = await controller.TestEmail(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TestEmail_WhenRecipientMissing_ReturnsBadRequest()
    {
        var controller = CreateController(
            Environments.Development,
            new PasswordResetEmailSettings { TestRecipientEmail = string.Empty });

        var result = await controller.TestEmail(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(badRequest.Value);
        Assert.Contains("TestRecipientEmail", payloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TestEmail_WhenSuccessful_ReturnsOk()
    {
        var settings = new PasswordResetEmailSettings { TestRecipientEmail = "dev@example.com" };
        var controller = CreateController(Environments.Development, settings);

        var result = await controller.TestEmail(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"success\":true", payloadJson, StringComparison.OrdinalIgnoreCase);
        _passwordResetEmailService.Verify(
            x => x.SendTestEmailAsync(settings.TestRecipientEmail, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TestEmail_WhenSmtpFails_ReturnsDetailedFailure()
    {
        var settings = new PasswordResetEmailSettings { TestRecipientEmail = "dev@example.com" };
        var controller = CreateController(Environments.Development, settings);
        _passwordResetEmailService
            .Setup(x => x.SendTestEmailAsync(settings.TestRecipientEmail, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP authentication failed"));

        var result = await controller.TestEmail(CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(error.Value);
        Assert.Contains("SMTP authentication failed", payloadJson, StringComparison.Ordinal);
    }

    private DebugController CreateController(string environmentName, PasswordResetEmailSettings settings)
    {
        return new DebugController(
            new TestWebHostEnvironment { EnvironmentName = environmentName },
            _passwordResetEmailService.Object,
            Options.Create(settings),
            NullLogger<DebugController>.Instance);
    }
}
