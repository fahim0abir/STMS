using TaskStatus = SmartTask.Domain.Enums.TaskStatus;
using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record TaskItemCreateDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public Guid? AssigneeId { get; init; }
    public DateTime? DueAt { get; init; }
};
