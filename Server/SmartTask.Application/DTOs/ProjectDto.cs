namespace SmartTask.Application.DTOs;

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string OwnerEmail { get; init; } = string.Empty;
};
