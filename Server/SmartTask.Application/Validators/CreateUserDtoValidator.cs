using FluentValidation;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;

namespace SmartTask.Application.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Name).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Name));
        RuleFor(x => x.Role)
            .Must(role => role is Role.ProjectManager or Role.TeamMember)
            .WithMessage("Only ProjectManager or TeamMember accounts can be created.");
    }
}