using SmartTask.Domain.Entities;

namespace SmartTask.Infrastructure.Auth;

public interface IJwtTokenService
{
    (string AccessToken, DateTime AccessTokenExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
