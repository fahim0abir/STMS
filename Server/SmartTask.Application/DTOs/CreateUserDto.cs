using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record CreateUserDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? Name { get; init; }
    public Role Role { get; init; } = Role.TeamMember;
};