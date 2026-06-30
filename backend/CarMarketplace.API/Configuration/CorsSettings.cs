namespace CarMarketplace.API.Configuration;

/// <summary>
/// CORS configuration for frontend origins.
/// </summary>
public class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Exact allowed origins for credentialed browser requests.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
