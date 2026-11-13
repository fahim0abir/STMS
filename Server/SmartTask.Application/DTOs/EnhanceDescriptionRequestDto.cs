namespace SmartTask.Application.DTOs;

public record EnhanceDescriptionRequestDto
{
    public string Description { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Context { get; init; }
}
