using SmartTask.Application.DTOs;
using SmartTask.Application.DTOs.Auth;

namespace SmartTask.Application.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterDto registerDto);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<(UserDto User, string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken)> LoginAsync(LoginDto loginDto);
    Task<(UserDto User, string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
