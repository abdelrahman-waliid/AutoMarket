using CarMarketplace.Application.DTOs;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for car creation payload.
/// </summary>
public class CreateCarRequestDTOValidator : AbstractValidator<CreateCarRequestDTO>
{
    private const int YearMin = 1900;

    public CreateCarRequestDTOValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.Year)
            .InclusiveBetween(YearMin, DateTime.UtcNow.Year)
            .WithMessage($"Year must be between {YearMin} and {DateTime.UtcNow.Year}.");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).WithMessage("Mileage must be greater than or equal to 0.");
    }
}
