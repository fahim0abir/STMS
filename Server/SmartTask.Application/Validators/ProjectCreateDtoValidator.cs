using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class ProjectCreateDtoValidator : AbstractValidator<ProjectCreateDto>
{
    public ProjectCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ClientName).MaximumLength(200);
        RuleFor(x => x.TechStack).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be one of: Active, OnHold, Completed, Archived.");
        RuleFor(x => x.ManagerId!.Value)
            .NotEmpty()
            .When(x => x.ManagerId.HasValue)
            .WithMessage("Selected project manager is invalid.");
    }
}
