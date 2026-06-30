using Microsoft.Extensions.Hosting;

namespace CarMarketplace.API.Configuration;

/// <summary>
/// Performs startup validation for password reset email settings.
/// </summary>
public static class PasswordResetEmailStartupValidator
{
    public static void ValidateOrWarn(
        PasswordResetEmailSettings settings,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var issues = new List<string>();
        ValidateRequiredSettings(settings, issues);
        ValidateGmailSettings(settings, issues);
        ValidateOperationalSettings(settings, issues);

        if (IsGmailHost(settings.SmtpHost))
        {
            logger.LogInformation(
                "Gmail SMTP configuration detected. Use port 587 with STARTTLS, and configure an App Password in PasswordResetEmail:SmtpPassword.");
        }

        if (issues.Count == 0)
        {
            logger.LogInformation(
                "Password reset email configuration loaded. Host={SmtpHost} Port={SmtpPort} EnableSsl={EnableSsl} FromEmail={FromEmail}",
                settings.SmtpHost,
                settings.SmtpPort,
                settings.EnableSsl,
                settings.FromEmail);
            return;
        }

        var combined = string.Join(" ", issues);
        if (environment.IsDevelopment())
        {
            logger.LogWarning(
                "Password reset email configuration issues detected (development mode). {Issues}",
                combined);
            return;
        }

        throw new InvalidOperationException(
            $"Password reset email configuration is invalid. {combined}");
    }

    private static void ValidateRequiredSettings(PasswordResetEmailSettings settings, ICollection<string> issues)
    {
        if (string.IsNullOrWhiteSpace(settings.FrontendBaseUrl))
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:FrontendBaseUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:SmtpHost is required.");
        }

        if (settings.SmtpPort is <= 0 or > 65535)
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:SmtpPort must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(settings.SmtpUsername))
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:SmtpUsername is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.SmtpPassword))
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:SmtpPassword is required.");
        }

        if (string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:FromEmail is required.");
        }
    }

    private static void ValidateGmailSettings(PasswordResetEmailSettings settings, ICollection<string> issues)
    {
        if (!IsGmailHost(settings.SmtpHost))
        {
            return;
        }

        if (settings.SmtpPort != 587)
        {
            issues.Add(
                $"{PasswordResetEmailSettings.SectionName}:SmtpPort must be 587 when using Gmail SMTP.");
        }

        if (!settings.EnableSsl)
        {
            issues.Add(
                $"{PasswordResetEmailSettings.SectionName}:EnableSsl must be true when using Gmail SMTP (STARTTLS).");
        }
    }

    private static void ValidateOperationalSettings(PasswordResetEmailSettings settings, ICollection<string> issues)
    {
        if (settings.TimeoutMilliseconds <= 0)
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:TimeoutMilliseconds must be greater than 0.");
        }

        if (settings.MaxRetryAttempts <= 0)
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:MaxRetryAttempts must be greater than 0.");
        }

        if (settings.RetryDelayMilliseconds < 0)
        {
            issues.Add($"{PasswordResetEmailSettings.SectionName}:RetryDelayMilliseconds must be 0 or greater.");
        }
    }

    private static bool IsGmailHost(string? host)
    {
        return !string.IsNullOrWhiteSpace(host)
            && host.Contains("smtp.gmail.com", StringComparison.OrdinalIgnoreCase);
    }
}
