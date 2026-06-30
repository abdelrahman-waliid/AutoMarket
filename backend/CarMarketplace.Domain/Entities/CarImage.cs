using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarMarketplace.Domain.Entities;

/// <summary>
/// Represents an image associated with a car listing.
/// </summary>
[Table("CarImages")]
public class CarImage
{
    /// <summary>
    /// Gets or sets the unique identifier for the car image.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the car this image belongs to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Car))]
    public Guid CarId { get; set; }

    /// <summary>
    /// Gets or sets the URL of the car image.
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Url]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the car image was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the car this image belongs to.
    /// </summary>
    [InverseProperty(nameof(Car.Images))]
    public virtual Car Car { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="CarImage"/> class.
    /// </summary>
    public CarImage()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CarImage"/> class with specified values.
    /// </summary>
    /// <param name="carId">The unique identifier of the car this image belongs to.</param>
    /// <param name="imageUrl">The URL of the car image.</param>
    public CarImage(Guid carId, string imageUrl)
        : this()
    {
        CarId = carId;
        ImageUrl = imageUrl;
    }
}
