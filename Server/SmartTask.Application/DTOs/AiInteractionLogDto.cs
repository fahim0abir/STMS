namespace SmartTask.Application.DTOs;

/// <summary>
/// A successful AI provider call as persisted for administrator review.
/// </summary>
public record AiInteractionLogDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string UserRole { get; init; } = string.Empty;
    public Guid? TaskId { get; init; }
    public string Model { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string InputText { get; init; } = string.Empty;
    public string OutputJson { get; init; } = string.Empty;
    public int DurationMs { get; init; }
    public DateTime CreatedAt { get; init; }
}
