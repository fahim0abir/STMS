namespace SmartTask.Application.DTOs;

public record AiInteractionLogCreateDto
{
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
}
