using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarMarketplace.Infrastructure.Data;

/// <summary>
/// Seeds initial data (e.g. default Admin user) when the application starts.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Default email for the seeded Admin user. Override via config if needed.
    /// </summary>
    public const string DefaultAdminEmail = "admin@carmarketplace.local";

    /// <summary>
    /// Default display name for the seeded Admin user.
    /// </summary>
    public const string DefaultAdminFullName = "System Administrator";

    /// <summary>
    /// Ensures at least one Admin user exists. If none exists, creates one with the given password.
    /// Call from host startup after building the app (using a scope).
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="adminPassword">Plain password for the new Admin user (only used when creating).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task EnsureAdminUserAsync(
        AppDbContext context,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        const int MinAdminPasswordLength = 12;
        if (string.IsNullOrWhiteSpace(adminPassword))
            throw new ArgumentException("Seed admin password must be provided.", nameof(adminPassword));
        if (adminPassword.Length < MinAdminPasswordLength)
            throw new ArgumentException($"Seed admin password must be at least {MinAdminPasswordLength} characters.", nameof(adminPassword));

        var hasAdmin = await context.Users
            .AnyAsync(u => u.Role == UserRole.Admin, cancellationToken);

        if (hasAdmin)
            return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, BCrypt.Net.BCrypt.GenerateSalt(12));

        var admin = new User
        {
            Id = Guid.NewGuid(),
            FullName = DefaultAdminFullName,
            Email = DefaultAdminEmail,
            PasswordHash = passwordHash,
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
    }
}
