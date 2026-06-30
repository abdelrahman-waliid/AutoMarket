namespace CarMarketplace.Application.Exceptions;

/// <summary>
/// Exception thrown when a previously rotated refresh token is reused, indicating potential token theft.
/// </summary>
public class RefreshTokenReuseDetectedException : Exception
{
    public RefreshTokenReuseDetectedException()
        : base("Refresh token reuse detected.")
    {
    }

    public RefreshTokenReuseDetectedException(string message)
        : base(message)
    {
    }
}
