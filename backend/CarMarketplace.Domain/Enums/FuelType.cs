namespace CarMarketplace.Domain.Enums;

/// <summary>
/// Represents the fuel type of a car.
/// </summary>
public enum FuelType
{
    /// <summary>
    /// Gasoline/Petrol fuel type.
    /// </summary>
    Gasoline = 0,

    /// <summary>
    /// Diesel fuel type.
    /// </summary>
    Diesel = 1,

    /// <summary>
    /// Electric vehicle.
    /// </summary>
    Electric = 2,

    /// <summary>
    /// Hybrid vehicle (gasoline-electric).
    /// </summary>
    Hybrid = 3,

    /// <summary>
    /// Plug-in hybrid vehicle.
    /// </summary>
    PlugInHybrid = 4,

    /// <summary>
    /// Compressed natural gas.
    /// </summary>
    CNG = 5
}
