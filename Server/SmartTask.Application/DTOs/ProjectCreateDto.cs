namespace SmartTask.Application.DTOs;

public record ProjectCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ManagerId { get; init; }
};
