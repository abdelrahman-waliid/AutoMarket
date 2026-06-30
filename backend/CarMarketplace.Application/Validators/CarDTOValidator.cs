using CarMarketplace.Application.DTOs;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for <see cref="CarDTO"/> (create and update).
/// </summary>
public class CarDTOValidator : AbstractValidator<CarDTO>
{
    private const int TitleMaxLength = 200;
    private const int BrandMaxLength = 100;
    private const int ModelMaxLength = 100;
    private const int LocationMaxLength = 200;
    private const int StatusMaxLength = 50;
    private const int DescriptionMaxLength = 2000;
    private const int YearMin = 1900;
    private const int YearMax = 2100;

    public CarDTOValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(TitleMaxLength).WithMessage($"Title must not exceed {TitleMaxLength} characters.");

        RuleFor(x => x.Brand)
            .MaximumLength(BrandMaxLength).WithMessage($"Brand must not exceed {BrandMaxLength} characters.");

        RuleFor(x => x.Model)
            .MaximumLength(ModelMaxLength).WithMessage($"Model must not exceed {ModelMaxLength} characters.");

        RuleFor(x => x.Location)
            .MaximumLength(LocationMaxLength).WithMessage($"Location must not exceed {LocationMaxLength} characters.");

        RuleFor(x => x.Status)
            .MaximumLength(StatusMaxLength).WithMessage($"Status must not exceed {StatusMaxLength} characters.");

        RuleFor(x => x)
            .Must(car =>
            {
                var hasTitle = !string.IsNullOrWhiteSpace(car.Title);
                var hasBrandModel = !string.IsNullOrWhiteSpace(car.Brand) && !string.IsNullOrWhiteSpace(car.Model);
                return hasTitle || hasBrandModel;
            })
            .WithMessage("Provide either Title, or both Brand and Model.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(DescriptionMaxLength).WithMessage($"Description must not exceed {DescriptionMaxLength} characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.Year)
            .InclusiveBetween(YearMin, YearMax).WithMessage($"Year must be between {YearMin} and {YearMax}.");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).WithMessage("Mileage must be 0 or greater.");

        // OwnerId required for update (when Id is set); for create the API sets it from the current user
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required.")
            .When(x => x.Id != Guid.Empty);
    }
}
