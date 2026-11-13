using FluentValidation;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Validators;

public class ProjectQueryDtoValidator : AbstractValidator<ProjectQueryDto>
{
    public ProjectQueryDtoValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber must be greater than or equal to 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
        RuleFor(x => x.SortOrder)
            .Must(x => string.Equals(x, "asc", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "desc", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortOrder))
            .WithMessage("SortOrder must be 'asc' or 'desc'.");
        RuleFor(x => x.SortBy)
            .Must((dto, sortBy) => string.IsNullOrWhiteSpace(sortBy) || ProjectQueryDto.AllowedSortFields.Contains(sortBy.Trim()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ProjectQueryDto.AllowedSortFields)}.");
    }
}
