namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Profile update payload for the currently authenticated user.
/// </summary>
public class ProfileUpdateRequestDTO
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
