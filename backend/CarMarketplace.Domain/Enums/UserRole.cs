namespace CarMarketplace.Domain.Enums;

/// <summary>
/// Represents the role of a user in the car marketplace system.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Default role for registered users; can manage own resources.
    /// </summary>
    User = 0,

    /// <summary>
    /// User who can list and sell cars.
    /// </summary>
    Seller = 1,

    /// <summary>
    /// Administrator with full system access (e.g. delete any car).
    /// </summary>
    Admin = 2
}
