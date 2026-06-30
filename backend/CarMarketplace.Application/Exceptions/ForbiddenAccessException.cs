namespace CarMarketplace.Application.Exceptions;

/// <summary>
/// Exception thrown when an authenticated user attempts to access a resource they do not own.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("You are not authorized to access this resource.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
