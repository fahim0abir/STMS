using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class TaskItemCreateDtoValidator : AbstractValidator<TaskItemCreateDto>
{
    public TaskItemCreateDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}
