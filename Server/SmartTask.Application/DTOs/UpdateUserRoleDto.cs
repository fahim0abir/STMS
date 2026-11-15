using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record UpdateUserRoleDto
{
    public Role Role { get; init; } = Role.ProjectManager;
}
