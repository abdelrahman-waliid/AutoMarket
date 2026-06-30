using CarMarketplace.Application.DTOs;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for profile updates.
/// </summary>
public class ProfileUpdateRequestDTOValidator : AbstractValidator<ProfileUpdateRequestDTO>
{
    private const int FullNameMaxLength = 200;
    private const int EmailMaxLength = 256;

    public ProfileUpdateRequestDTOValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(FullNameMaxLength).WithMessage($"Full name must not exceed {FullNameMaxLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
    }
}
