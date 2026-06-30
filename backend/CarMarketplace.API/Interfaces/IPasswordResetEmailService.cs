namespace CarMarketplace.API.Interfaces;

/// <summary>
/// Sends password reset instructions to users.
/// </summary>
public interface IPasswordResetEmailService
{
    /// <summary>
    /// Sends password reset instructions containing the one-time reset token.
    /// </summary>
    /// <param name="recipientEmail">Recipient email address.</param>
    /// <param name="resetToken">Opaque one-time reset token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetAsync(string recipientEmail, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a diagnostics test email to verify SMTP connectivity.
    /// </summary>
    /// <param name="recipientEmail">Recipient email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendTestEmailAsync(string recipientEmail, CancellationToken cancellationToken = default);
}
