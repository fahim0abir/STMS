using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public record AuthResultDto
{
    public UserDto User { get; init; } = null!;
    public string AccessToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
};
