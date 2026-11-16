using TaskStatus = SmartTask.Domain.Enums.TaskStatus;
using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record TaskItemDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public Guid? AssigneeId { get; init; }
    public string? AssigneeEmail { get; init; }
    public string? AssigneeName { get; init; }
    public DateTime? DueAt { get; init; }
    public string? AiSuggestionJson { get; init; }
    public string? AiSuggestedDescription { get; init; }
    public string? AiSuggestedTitle { get; init; }
    public string? AiModel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
};
