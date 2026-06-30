using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Application.DTOs;

/// <summary>
/// Admin role update payload.
/// </summary>
public class UpdateUserRoleRequestDTO
{
    public UserRole Role { get; set; }
}
