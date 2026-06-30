using CarMarketplace.Domain.Entities;

namespace CarMarketplace.Application.Interfaces;

/// <summary>
/// Repository abstraction for password reset token persistence.
/// </summary>
public interface IPasswordResetTokenRepository
{
    void Add(PasswordResetToken token);

    Task<PasswordResetToken?> GetByTokenHashTrackedAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<int> InvalidateActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
