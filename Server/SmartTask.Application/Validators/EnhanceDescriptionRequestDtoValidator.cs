using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class EnhanceDescriptionRequestDtoValidator : AbstractValidator<EnhanceDescriptionRequestDto>
{
    public EnhanceDescriptionRequestDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(4000);

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.Context)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Context));
    }
}
