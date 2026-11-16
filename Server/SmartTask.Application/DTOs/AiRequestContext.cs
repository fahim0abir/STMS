namespace SmartTask.Application.DTOs;

/// <summary>
/// Caller identity attached to an AI request so provider calls can be audited.
/// </summary>
public record AiRequestContext
{
    public Guid? UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string UserRole { get; init; } = string.Empty;
    public Guid? TaskId { get; init; }
}
