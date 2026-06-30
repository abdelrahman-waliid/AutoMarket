using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CarMarketplace.API.Services;

/// <summary>
/// Ensures SignalR user identity maps to the same JWT claim used by the REST API.
/// </summary>
public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
