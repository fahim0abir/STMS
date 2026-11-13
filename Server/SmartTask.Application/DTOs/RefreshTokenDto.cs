namespace SmartTask.Application.DTOs;

public record RefreshTokenDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
};
