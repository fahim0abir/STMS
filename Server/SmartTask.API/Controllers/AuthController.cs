using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartTask.Application.DTOs;
using SmartTask.Application.DTOs.Auth;
using SmartTask.Application.Interfaces;

namespace SmartTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserDto>>> RegisterAsync([FromBody] RegisterDto registerDto)
    {
        var user = await _authService.RegisterAsync(registerDto);
        return Ok(ApiResponse<UserDto>.Ok(user, "User registered successfully."));
    }

    [HttpGet("email-availability")]
    [AllowAnonymous]
    [EnableRateLimiting("default")]
    public async Task<ActionResult<ApiResponse<EmailAvailabilityDto>>> CheckEmailAvailabilityAsync([FromQuery] string? email)
    {
        var available = await _authService.IsEmailAvailableAsync(email ?? string.Empty);
        return Ok(ApiResponse<EmailAvailabilityDto>.Ok(new EmailAvailabilityDto { Available = available }));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> LoginAsync([FromBody] LoginDto loginDto)
    {
        var (user, accessToken, accessTokenExpiresAt, refreshToken) = await _authService.LoginAsync(loginDto);
        var response = new AuthResultDto
        {
            User = user,
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = refreshToken
        };
        return Ok(ApiResponse<AuthResultDto>.Ok(response, "Login successful."));
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> RefreshTokenAsync([FromBody] string refreshToken)
    {
        var (user, accessToken, accessTokenExpiresAt, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
        var response = new AuthResultDto
        {
            User = user,
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = newRefreshToken
        };
        return Ok(ApiResponse<AuthResultDto>.Ok(response, "Token refreshed."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAsync([FromBody] string refreshToken)
    {
        await _authService.LogoutAsync(refreshToken);
        return Ok(ApiResponse<object>.Ok(null, "Logged out."));
    }
}
