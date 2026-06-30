namespace CarMarketplace.API.Configuration;

/// <summary>
/// SMTP + frontend URL settings used for password reset emails.
/// </summary>
public class PasswordResetEmailSettings
{
    public const string SectionName = "PasswordResetEmail";

    /// <summary>
    /// Frontend base URL used to build links like /reset-password?token=...
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "https://frontend";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; } = 25;

    /// <summary>
    /// Whether SMTP should use SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// Optional SMTP username.
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// Optional SMTP password.
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Sender address displayed in reset emails.
    /// </summary>
    public string FromEmail { get; set; } = "no-reply@carmarketplace.local";

    /// <summary>
    /// Optional sender name displayed in reset emails.
    /// </summary>
    public string? FromName { get; set; } = "Car Marketplace";

    /// <summary>
    /// SMTP operation timeout in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 10000;

    /// <summary>
    /// Number of retry attempts for transient SMTP failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Optional diagnostics recipient for /api/debug/test-email.
    /// </summary>
    public string? TestRecipientEmail { get; set; }
}
