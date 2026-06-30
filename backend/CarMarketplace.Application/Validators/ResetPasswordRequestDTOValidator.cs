using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Security;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for password reset completion requests.
/// </summary>
public class ResetPasswordRequestDTOValidator : AbstractValidator<ResetPasswordRequestDTO>
{
    public ResetPasswordRequestDTOValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters.")
            .Must(PasswordPolicy.MeetsComplexity)
            .WithMessage(PasswordPolicy.ComplexityMessage);
    }
}
