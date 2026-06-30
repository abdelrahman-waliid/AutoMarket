using CarMarketplace.API.Configuration;
using CarMarketplace.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CarMarketplace.API.Controllers;

/// <summary>
/// Development diagnostics endpoints.
/// </summary>
[ApiController]
[Route("api/debug")]
[AllowAnonymous]
public sealed class DebugController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IPasswordResetEmailService _passwordResetEmailService;
    private readonly PasswordResetEmailSettings _settings;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        IWebHostEnvironment environment,
        IPasswordResetEmailService passwordResetEmailService,
        IOptions<PasswordResetEmailSettings> options,
        ILogger<DebugController> logger)
    {
        _environment = environment;
        _passwordResetEmailService = passwordResetEmailService;
        _settings = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends a test email using current SMTP configuration.
    /// Available in Development only.
    /// </summary>
    [HttpGet("test-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestEmail(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(_settings.TestRecipientEmail))
        {
            return BadRequest(new
            {
                success = false,
                error = $"{PasswordResetEmailSettings.SectionName}:TestRecipientEmail is not configured."
            });
        }

        try
        {
            await _passwordResetEmailService.SendTestEmailAsync(_settings.TestRecipientEmail, cancellationToken);
            return Ok(new
            {
                success = true,
                message = $"Test email sent to {_settings.TestRecipientEmail}."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP diagnostics email failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name,
                innerException = ex.InnerException?.Message
            });
        }
    }
}
