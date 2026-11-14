using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class TaskAssigneeAssignDtoValidator : AbstractValidator<TaskAssigneeAssignDto>
{
    public TaskAssigneeAssignDtoValidator()
    {
        RuleFor(x => x.AssigneeId!.Value)
            .NotEmpty()
            .When(x => x.AssigneeId.HasValue)
            .WithMessage("Selected assignee is invalid.");
    }
}