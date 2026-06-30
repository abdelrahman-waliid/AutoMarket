namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Request payload to initiate password reset.
/// </summary>
public class ForgotPasswordRequestDTO
{
    public string Email { get; set; } = string.Empty;
}
