using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Security;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for authenticated password change requests.
/// </summary>
public class ChangePasswordRequestDTOValidator : AbstractValidator<ChangePasswordRequestDTO>
{
    public ChangePasswordRequestDTOValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters.")
            .Must(PasswordPolicy.MeetsComplexity).WithMessage(PasswordPolicy.ComplexityMessage);

        RuleFor(x => x)
            .Must(x => x.CurrentPassword != x.NewPassword)
            .WithMessage("New password must be different from the current password.")
            .When(x => !string.IsNullOrWhiteSpace(x.CurrentPassword) && !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}
