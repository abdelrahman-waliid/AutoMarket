using System.Net;
using System.Text.Json;
using CarMarketplace.API.Middleware;
using CarMarketplace.Application.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CarMarketplace.Tests.Integration;

public class ExceptionHandlingMiddlewareIntegrationTests
{
    [Fact]
    public async Task InvalidOperationException_Returns400ProblemDetails()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-invalid");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response, body, 400, "Bad Request", "invalid_operation", "Invalid operation.");
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns401ProblemDetails()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-unauthorized");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertProblemDetails(response, body, 401, "Unauthorized", "unauthorized", "Authentication is required.");
    }

    [Fact]
    public async Task AuthorizationException_Returns403ProblemDetails()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-authorization-forbidden");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertProblemDetails(response, body, 403, "Forbidden", "authorization_error", "Forbidden operation.");
    }

    [Fact]
    public async Task AuthorizationExceptionUnauthorized_Returns401ProblemDetails()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-authorization-unauthorized");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertProblemDetails(response, body, 401, "Unauthorized", "authorization_error", "Missing auth.");
    }

    [Fact]
    public async Task AccountLockedException_Returns403ProblemDetails()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-account-locked");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(403, body.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", body.GetProperty("title").GetString());
        Assert.Equal("account_locked", body.GetProperty("errorCode").GetString());
        Assert.True(body.GetProperty("detail").GetString()!.Contains("Account is locked until", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404ProblemDetails()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-notfound");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response, body, 404, "Not Found", "not_found", "Resource not found.");
    }

    [Fact]
    public async Task UnknownException_Production_Returns500WithoutInternalMessage()
    {
        var client = CreateClient(Environments.Production);
        var response = await client.GetAsync("/throw-unknown");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertProblemDetails(
            response,
            body,
            500,
            "Internal Server Error",
            "internal_server_error",
            "An unexpected error occurred.");
        Assert.False(body.TryGetProperty("exceptionType", out _));
    }

    [Fact]
    public async Task UnknownException_Development_IncludesExceptionType()
    {
        var client = CreateClient(Environments.Development);
        var response = await client.GetAsync("/throw-unknown");
        var body = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Exception", body.GetProperty("exceptionType").GetString());
    }

    private static HttpClient CreateClient(string environmentName)
    {
        var hostBuilder = new WebHostBuilder()
            .UseEnvironment(environmentName)
            .Configure(app =>
            {
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.Map("/throw-invalid", endpoint =>
                {
                    endpoint.Run(_ => throw new InvalidOperationException("Invalid operation."));
                });

                app.Map("/throw-unauthorized", endpoint =>
                {
                    endpoint.Run(_ => throw new UnauthorizedAccessException("Authentication is required."));
                });

                app.Map("/throw-authorization-forbidden", endpoint =>
                {
                    endpoint.Run(_ => throw new AuthorizationException("Forbidden operation."));
                });

                app.Map("/throw-authorization-unauthorized", endpoint =>
                {
                    endpoint.Run(_ => throw new AuthorizationException("Missing auth.", AuthorizationException.UnauthorizedStatusCode));
                });

                app.Map("/throw-account-locked", endpoint =>
                {
                    endpoint.Run(_ => throw new AccountLockedException(DateTime.UtcNow.AddMinutes(15)));
                });

                app.Map("/throw-notfound", endpoint =>
                {
                    endpoint.Run(_ => throw new KeyNotFoundException("Resource not found."));
                });

                app.Map("/throw-unknown", endpoint =>
                {
                    endpoint.Run(_ => throw new Exception("Sensitive internals"));
                });
            });

        var server = new TestServer(hostBuilder);
        return server.CreateClient();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }

    private static void AssertProblemDetails(
        HttpResponseMessage response,
        JsonElement body,
        int expectedStatus,
        string expectedTitle,
        string expectedErrorCode,
        string expectedDetail)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedStatus, body.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, body.GetProperty("title").GetString());
        Assert.Equal(expectedErrorCode, body.GetProperty("errorCode").GetString());
        Assert.Equal(expectedDetail, body.GetProperty("detail").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }
}
