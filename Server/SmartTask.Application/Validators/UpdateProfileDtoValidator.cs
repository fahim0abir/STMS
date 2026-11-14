using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Name));
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword))
            .WithMessage("Current password is required to set a new password.");
        RuleFor(x => x.NewPassword)
            .MinimumLength(8)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirm the new password.")
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.")
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}