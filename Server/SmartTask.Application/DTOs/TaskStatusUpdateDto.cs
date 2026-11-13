using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Application.DTOs;

/// <summary>
/// Payload for the lightweight task status update endpoint.
/// </summary>
public record TaskStatusUpdateDto
{
    public TaskStatus Status { get; init; }
}
