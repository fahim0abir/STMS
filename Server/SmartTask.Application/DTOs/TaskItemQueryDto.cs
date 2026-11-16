using TaskStatus = SmartTask.Domain.Enums.TaskStatus;
using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public class TaskItemQueryDto
{
    public const string DefaultSortBy = "CreatedAt";
    public const string DefaultSortOrder = "desc";

    public static readonly string[] AllowedSortFields = { "Title", "Status", "Priority", "DueAt", "CreatedAt" };

    public string? Keyword { get; init; }
    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public Guid? ProjectId { get; init; }
    public bool PendingOnly { get; init; }
    public Guid? AssigneeId { get; init; }
    public DateTime? DueFrom { get; init; }
    public DateTime? DueTo { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; } = DefaultSortOrder;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
