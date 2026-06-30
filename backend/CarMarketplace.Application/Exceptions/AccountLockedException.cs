namespace CarMarketplace.Application.Exceptions;

/// <summary>
/// Exception thrown when a login is attempted for an account that is currently locked.
/// </summary>
public class AccountLockedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountLockedException"/> class.
    /// </summary>
    /// <param name="lockoutEnd">The UTC timestamp when the lockout ends.</param>
    public AccountLockedException(DateTime lockoutEnd)
        : base($"Account is locked until {lockoutEnd:O}.")
    {
        LockoutEnd = lockoutEnd;
    }

    /// <summary>
    /// Gets the UTC timestamp when the lockout ends.
    /// </summary>
    public DateTime LockoutEnd { get; }
}
