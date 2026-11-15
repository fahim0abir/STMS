using FluentValidation;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;

namespace SmartTask.Application.Validators;

public class UpdateUserRoleDtoValidator : AbstractValidator<UpdateUserRoleDto>
{
    public UpdateUserRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .Must(role => role == Role.ProjectManager)
            .WithMessage("Only promotion to ProjectManager is supported.");
    }
}
