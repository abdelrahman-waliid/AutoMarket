namespace CarMarketplace.Application.Exceptions;

/// <summary>
/// Exception representing authorization failures with explicit HTTP status semantics.
/// </summary>
public class AuthorizationException : Exception
{
    public const int UnauthorizedStatusCode = 401;
    public const int ForbiddenStatusCode = 403;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
    /// </summary>
    public AuthorizationException(string message, int statusCode = ForbiddenStatusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code associated with this authorization failure.
    /// </summary>
    public int StatusCode { get; }
}
