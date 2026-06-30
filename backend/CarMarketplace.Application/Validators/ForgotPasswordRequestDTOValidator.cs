using CarMarketplace.Application.DTOs;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for forgot password requests.
/// </summary>
public class ForgotPasswordRequestDTOValidator : AbstractValidator<ForgotPasswordRequestDTO>
{
    private const int EmailMaxLength = 256;

    public ForgotPasswordRequestDTOValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
    }
}
