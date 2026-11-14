using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class ProjectManagerAssignDtoValidator : AbstractValidator<ProjectManagerAssignDto>
{
    public ProjectManagerAssignDtoValidator()
    {
        RuleFor(x => x.ManagerId).NotEmpty().WithMessage("Select a project manager.");
    }
}