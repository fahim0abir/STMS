using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class EnhanceDescriptionRequestDtoValidator : AbstractValidator<EnhanceDescriptionRequestDto>
{
    public EnhanceDescriptionRequestDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(1000);
    }
}
