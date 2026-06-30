using Microsoft.EntityFrameworkCore;
using CarMarketplace.Domain.Entities;
using CarMarketplace.Domain.Enums;

namespace CarMarketplace.Infrastructure.Data;

/// <summary>
/// Represents the database context for the Car Marketplace application.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the users in the database.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cars in the database.
    /// </summary>
    public DbSet<Car> Cars { get; set; } = null!;

    /// <summary>
    /// Gets or sets the car images in the database.
    /// </summary>
    public DbSet<CarImage> CarImages { get; set; } = null!;

    /// <summary>
    /// Gets or sets the messages in the database.
    /// </summary>
    public DbSet<Message> Messages { get; set; } = null!;

    /// <summary>
    /// Gets or sets the refresh tokens in the database.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets password reset tokens in the database.
    /// </summary>
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    /// <summary>
    /// Configures the model relationships and constraints using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User -> Car relationship (One-to-Many)
        modelBuilder.Entity<Car>()
            .HasOne(c => c.Owner)
            .WithMany(u => u.AddedCars)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Car -> CarImage relationship (One-to-Many)
        modelBuilder.Entity<CarImage>()
            .HasOne(ci => ci.Car)
            .WithMany(c => c.Images)
            .HasForeignKey(ci => ci.CarId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Message -> User relationships (Many-to-One for both Sender and Receiver)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Car)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.CarId)
            .OnDelete(DeleteBehavior.SetNull);

        // Global query filters for soft-delete behavior.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        modelBuilder.Entity<Car>()
            .HasQueryFilter(c => !c.IsDeleted);

        modelBuilder.Entity<CarImage>()
            .HasQueryFilter(ci => !ci.Car.IsDeleted);

        modelBuilder.Entity<Message>()
            .HasQueryFilter(m => !m.IsDeleted);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(rt => !rt.User.IsDeleted);

        modelBuilder.Entity<PasswordResetToken>()
            .HasQueryFilter(prt => !prt.User.IsDeleted);

        // Configure indexes for better query performance
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(UserRole.User);

        modelBuilder.Entity<User>()
            .Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        modelBuilder.Entity<User>()
            .Property(u => u.SecurityStamp)
            .HasMaxLength(64)
            .HasDefaultValueSql("CONVERT(nvarchar(36), NEWID())");

        modelBuilder.Entity<User>()
            .Property(u => u.IsOnline)
            .HasDefaultValue(false);

        modelBuilder.Entity<User>()
            .Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<Car>()
            .Property(c => c.Status)
            .HasDefaultValue("Active");

        modelBuilder.Entity<Car>()
            .Property(c => c.Views)
            .HasDefaultValue(0);

        modelBuilder.Entity<Car>()
            .Property(c => c.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.OwnerId);

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.Brand);

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.CreatedAt);

        modelBuilder.Entity<CarImage>()
            .HasIndex(ci => ci.CarId);

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.SenderId);

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.ReceiverId);

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ReceiverId, m.SenderId, m.IsSeen });

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ReceiverId, m.IsDelivered });

        modelBuilder.Entity<Message>()
            .Property(m => m.IsRead)
            .HasDefaultValue(false);

        modelBuilder.Entity<Message>()
            .Property(m => m.IsDelivered)
            .HasDefaultValue(false);

        modelBuilder.Entity<Message>()
            .Property(m => m.IsSeen)
            .HasDefaultValue(false);

        modelBuilder.Entity<Message>()
            .Property(m => m.IsDeleted)
            .HasDefaultValue(false);

        // Message IDs are generated by the application layer to guarantee consistency.
        modelBuilder.Entity<Message>()
            .Property(m => m.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.CarId);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(prt => prt.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(prt => prt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .Property(prt => prt.IsUsed)
            .HasDefaultValue(false);
    }
}
