using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class TaskItemQueryDtoValidator : AbstractValidator<TaskItemQueryDto>
{
    public TaskItemQueryDtoValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be greater than or equal to 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x.SortOrder)
            .Must(x => string.Equals(x, "asc", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "desc", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortOrder))
            .WithMessage("SortOrder must be 'asc' or 'desc'.");
        RuleFor(x => x.SortBy)
            .Must((dto, sortBy) => string.IsNullOrWhiteSpace(sortBy) || TaskItemQueryDto.AllowedSortFields.Contains(sortBy.Trim()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", TaskItemQueryDto.AllowedSortFields)}.");
        RuleFor(x => x.DueTo)
            .GreaterThanOrEqualTo(x => x.DueFrom)
            .When(x => x.DueFrom.HasValue && x.DueTo.HasValue)
            .WithMessage("DueTo must be greater than or equal to DueFrom.");
    }
}
