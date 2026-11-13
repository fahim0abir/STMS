namespace SmartTask.Application.DTOs.Auth;

public record RegisterDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? Name { get; init; }
};
