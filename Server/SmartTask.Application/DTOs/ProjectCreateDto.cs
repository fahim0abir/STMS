using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record ProjectCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ClientName { get; init; }
    public string TechStack { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public ProjectStatus Status { get; init; } = ProjectStatus.Active;
    public Guid? ManagerId { get; init; }
};
