namespace CarMarketplace.API.Configuration;

/// <summary>
/// Trusted reverse-proxy configuration for forwarded headers.
/// </summary>
public class ReverseProxySettings
{
    public const string SectionName = "ReverseProxy";

    /// <summary>
    /// Trusted proxy IPs (e.g. 10.0.0.10).
    /// </summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>
    /// Trusted proxy networks in CIDR notation (e.g. 10.0.0.0/24).
    /// </summary>
    public string[] KnownNetworks { get; set; } = [];
}
