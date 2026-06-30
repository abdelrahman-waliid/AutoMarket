using CarMarketplace.Application.DTOs;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for <see cref="LoginRequestDTO"/>.
/// </summary>
public class LoginRequestDTOValidator : AbstractValidator<LoginRequestDTO>
{
    private const int EmailMaxLength = 256;

    public LoginRequestDTOValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
