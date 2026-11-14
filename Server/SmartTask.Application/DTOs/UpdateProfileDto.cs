namespace SmartTask.Application.DTOs;

public record UpdateProfileDto
{
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? CurrentPassword { get; init; }
    public string? NewPassword { get; init; }
    public string? ConfirmPassword { get; init; }
};