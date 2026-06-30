namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Request payload for an authenticated password change.
/// </summary>
public class ChangePasswordRequestDTO
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
