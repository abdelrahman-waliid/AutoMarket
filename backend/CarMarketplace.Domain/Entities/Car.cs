using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Domain.Entities;

/// <summary>
/// Represents a car listing in the marketplace.
/// </summary>
[Table("Cars")]
public class Car
{
    /// <summary>
    /// Gets or sets the unique identifier for the car.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the car listing.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the brand of the car.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model of the car.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the car.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the price of the car.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the location of the listing.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the listing status.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets how many times the listing has been viewed.
    /// </summary>
    [Required]
    public int Views { get; set; }

    /// <summary>
    /// Gets or sets the manufacturing year of the car.
    /// </summary>
    [Required]
    [Range(1900, 2100)]
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the mileage of the car in kilometers or miles.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }

    /// <summary>
    /// Gets or sets the fuel type of the car.
    /// </summary>
    [Required]
    public FuelType FuelType { get; set; }

    /// <summary>
    /// Gets or sets the transmission type of the car.
    /// </summary>
    [Required]
    public TransmissionType TransmissionType { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the car owner.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Owner))]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the car listing was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the car listing was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this listing was soft-deleted.
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the owner of the car.
    /// </summary>
    [InverseProperty(nameof(User.AddedCars))]
    public virtual User Owner { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of images associated with this car.
    /// </summary>
    public virtual ICollection<CarImage> Images { get; set; } = new List<CarImage>();

    /// <summary>
    /// Gets or sets the collection of messages linked to this car.
    /// </summary>
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Car"/> class.
    /// </summary>
    public Car()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Status = "Active";
        Views = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Car"/> class with specified values.
    /// </summary>
    /// <param name="title">The title of the car listing.</param>
    /// <param name="description">The description of the car.</param>
    /// <param name="price">The price of the car.</param>
    /// <param name="year">The manufacturing year of the car.</param>
    /// <param name="mileage">The mileage of the car.</param>
    /// <param name="fuelType">The fuel type of the car.</param>
    /// <param name="transmissionType">The transmission type of the car.</param>
    /// <param name="ownerId">The unique identifier of the car owner.</param>
    public Car(string title, string description, decimal price, int year, int mileage, FuelType fuelType, TransmissionType transmissionType, Guid ownerId)
        : this()
    {
        Title = title;
        Brand = title;
        Model = title;
        Description = description;
        Price = price;
        Location = string.Empty;
        Year = year;
        Mileage = mileage;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        OwnerId = ownerId;
    }
}
