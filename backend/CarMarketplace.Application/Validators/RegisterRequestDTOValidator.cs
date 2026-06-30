using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Security;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for <see cref="RegisterRequestDTO"/>.
/// </summary>
public class RegisterRequestDTOValidator : AbstractValidator<RegisterRequestDTO>
{
    private const int FullNameMaxLength = 200;
    private const int EmailMaxLength = 256;

    public RegisterRequestDTOValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(FullNameMaxLength).WithMessage($"Full name must not exceed {FullNameMaxLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters.")
            .Must(PasswordPolicy.MeetsComplexity).WithMessage(PasswordPolicy.ComplexityMessage);
    }
}
