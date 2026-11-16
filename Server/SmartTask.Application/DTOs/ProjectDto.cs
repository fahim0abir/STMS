using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ClientName { get; init; }
    public string TechStack { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public ProjectStatus Status { get; init; }
    public Guid? ManagerId { get; init; }
    public string ManagerEmail { get; init; } = string.Empty;
    public string? ManagerName { get; init; }
};
