using System.Net;
using System.Net.Mail;
using CarMarketplace.API.Configuration;
using CarMarketplace.API.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CarMarketplace.API.Services;

/// <summary>
/// Sends password reset instructions using SMTP.
/// </summary>
public sealed class SmtpPasswordResetEmailService : IPasswordResetEmailService
{
    private readonly PasswordResetEmailSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SmtpPasswordResetEmailService> _logger;

    public SmtpPasswordResetEmailService(
        IOptions<PasswordResetEmailSettings> options,
        IWebHostEnvironment environment,
        ILogger<SmtpPasswordResetEmailService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail) || string.IsNullOrWhiteSpace(resetToken))
        {
            return;
        }

        var resetLink = BuildResetLink(resetToken);
        var subject = "Reset your Car Marketplace password";
        var body = BuildPasswordResetEmailBody(resetLink);
        await SendEmailCoreAsync(
            recipientEmail,
            subject,
            body,
            logResetLinkOnFailureInDevelopment: true,
            resetLinkForDevelopmentFallback: resetLink,
            cancellationToken);
    }

    public async Task SendTestEmailAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new InvalidOperationException(
                $"{PasswordResetEmailSettings.SectionName}:TestRecipientEmail is required to run diagnostics.");
        }

        var subject = "Car Marketplace SMTP diagnostics";
        var body =
            $"SMTP diagnostics email sent at {DateTime.UtcNow:O}.{Environment.NewLine}" +
            "If you received this message, SMTP connectivity is working.";
        await SendEmailCoreAsync(
            recipientEmail,
            subject,
            body,
            logResetLinkOnFailureInDevelopment: false,
            resetLinkForDevelopmentFallback: null,
            cancellationToken);
    }

    private async Task SendEmailCoreAsync(
        string recipientEmail,
        string subject,
        string body,
        bool logResetLinkOnFailureInDevelopment,
        string? resetLinkForDevelopmentFallback,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, _settings.MaxRetryAttempts);
        var retryDelay = TimeSpan.FromMilliseconds(Math.Max(0, _settings.RetryDelayMilliseconds));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var emailType = logResetLinkOnFailureInDevelopment ? "password reset" : "diagnostics";

            using var message = CreateMailMessage(recipientEmail, subject, body);
            using var smtpClient = CreateSmtpClient();

            _logger.LogInformation(
                "Sending {EmailType} email to {RecipientEmail}. Subject={Subject}. Attempt {Attempt}/{MaxAttempts}. SMTP {SmtpHost}:{SmtpPort} SSL={EnableSsl}",
                emailType,
                recipientEmail,
                subject,
                attempt,
                maxAttempts,
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.EnableSsl);

            try
            {
                await smtpClient.SendMailAsync(message);
                _logger.LogInformation(
                    "SMTP connected to {SmtpHost}:{SmtpPort}.",
                    _settings.SmtpHost,
                    _settings.SmtpPort);
                _logger.LogInformation("Email sent successfully to {RecipientEmail}.", recipientEmail);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                LogSmtpFailure(ex, recipientEmail, attempt, maxAttempts, willRetry: true);
                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                LogSmtpFailure(ex, recipientEmail, attempt, maxAttempts, willRetry: false);

                if (_environment.IsDevelopment()
                    && logResetLinkOnFailureInDevelopment
                    && !string.IsNullOrWhiteSpace(resetLinkForDevelopmentFallback))
                {
                    _logger.LogWarning("RESET LINK: {ResetLink}", resetLinkForDevelopmentFallback);
                    return;
                }

                throw;
            }
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Timeout = Math.Max(1, _settings.TimeoutMilliseconds),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);
        }

        return client;
    }

    private MailMessage CreateMailMessage(string recipientEmail, string subject, string body)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(recipientEmail);
        return message;
    }

    private void LogSmtpFailure(Exception exception, string recipientEmail, int attempt, int maxAttempts, bool willRetry)
    {
        _logger.LogError(
            exception,
            "Email failed for {RecipientEmail}. Attempt {Attempt}/{MaxAttempts}. SMTP {SmtpHost}:{SmtpPort}. WillRetry={WillRetry}",
            recipientEmail,
            attempt,
            maxAttempts,
            _settings.SmtpHost,
            _settings.SmtpPort,
            willRetry);

        if (IsGmailHost(_settings.SmtpHost) && IsAuthenticationFailure(exception))
        {
            _logger.LogError(
                "Gmail SMTP authentication failed. Ensure 2-Step Verification is enabled and PasswordResetEmail:SmtpPassword is an App Password (not your normal Gmail password).");
        }
    }

    private string BuildResetLink(string resetToken)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(resetToken);
        return $"{baseUrl}/reset-password?token={encodedToken}";
    }

    private static string BuildPasswordResetEmailBody(string resetLink)
    {
        return
            $"We received a request to reset your password.{Environment.NewLine}{Environment.NewLine}" +
            $"Use this link to continue:{Environment.NewLine}{resetLink}{Environment.NewLine}{Environment.NewLine}" +
            "If you did not request this, you can ignore this email.";
    }

    private static bool IsGmailHost(string? host)
    {
        return !string.IsNullOrWhiteSpace(host)
            && host.Contains("smtp.gmail.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        if (exception is SmtpException smtpException)
        {
            if (smtpException.StatusCode == SmtpStatusCode.GeneralFailure)
            {
                return true;
            }
        }

        return exception.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase)
            || (exception.InnerException?.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
