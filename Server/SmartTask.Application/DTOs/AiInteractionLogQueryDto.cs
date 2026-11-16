namespace SmartTask.Application.DTOs;

public class AiInteractionLogQueryDto
{
    public string? Keyword { get; init; }
    public string? Model { get; init; }
    public Guid? UserId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
