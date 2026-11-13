using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
    public Role Role { get; init; }
};
