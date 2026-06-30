using CarMarketplace.Application.DTOs;
using FluentValidation;

namespace CarMarketplace.Application.Validators;

/// <summary>
/// Validator for <see cref="MessageDTO"/> (e.g. when sending a message).
/// </summary>
public class MessageDTOValidator : AbstractValidator<MessageDTO>
{
    private const int ContentMaxLength = 1000;

    public MessageDTOValidator()
    {
        RuleFor(x => x.ReceiverId)
            .NotEmpty().WithMessage("Receiver ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required.")
            .MaximumLength(ContentMaxLength).WithMessage($"Content must not exceed {ContentMaxLength} characters.");
    }
}
