using TaskStatus = SmartTask.Domain.Enums.TaskStatus;
using SmartTask.Domain.Enums;

namespace SmartTask.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueAt { get; set; }
}
