namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Request payload to complete password reset using a one-time token.
/// </summary>
public class ResetPasswordRequestDTO
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
