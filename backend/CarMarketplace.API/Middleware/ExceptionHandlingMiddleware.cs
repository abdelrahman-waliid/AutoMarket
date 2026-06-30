using CarMarketplace.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CarMarketplace.API.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    ex,
                    "Unhandled exception occurred after response started. Path: {Path}",
                    context.Request.Path);
                throw;
            }

            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errorCode, detail) = MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception mapped to {StatusCode}. Path: {Path}. TraceId: {TraceId}",
            statusCode,
            context.Request.Path,
            context.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problem.Extensions["errorCode"] = errorCode;
        }

        if (_environment.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().Name;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem,
            SerializerOptions,
            context.RequestAborted);
    }

    private (int StatusCode, string Title, string ErrorCode, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException ex => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "invalid_operation",
                ex.Message),

            UnauthorizedAccessException ex => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "unauthorized",
                ex.Message),

            AccountLockedException ex => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "account_locked",
                ex.Message),

            ForbiddenAccessException ex => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "forbidden",
                ex.Message),

            AuthorizationException ex when ex.StatusCode == AuthorizationException.UnauthorizedStatusCode => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "authorization_error",
                ex.Message),

            AuthorizationException ex => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "authorization_error",
                ex.Message),

            KeyNotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "not_found",
                ex.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "internal_server_error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.")
        };
    }
}
