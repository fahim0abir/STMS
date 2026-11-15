using FluentValidation;
using SmartTask.Application.DTOs.Auth;

namespace SmartTask.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
