namespace SmartTask.Domain.Entities;

/// <summary>
/// Audit record of a successful AI provider call, kept for administrator review.
/// </summary>
public class AiInteractionLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public Guid? TaskId { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string InputText { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
