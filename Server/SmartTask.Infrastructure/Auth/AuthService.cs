using System;
using System.Collections.Generic;
using System.Security.Authentication;
using SmartTask.Application.DTOs;
using SmartTask.Application.DTOs.Auth;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Enums;
using SmartTask.Domain.Entities;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Persistence;
using SmartTask.Infrastructure.Repositories;
using BCrypt.Net;

namespace SmartTask.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IRepository<User> userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
    {
        if (await _userRepository.FindSingleAsync(u => u.Email.Equals(registerDto.Email, StringComparison.OrdinalIgnoreCase)) != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(registerDto.Password),
            Name = registerDto.Name,
            Role = Role.TeamMember,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return new UserDto { Id = user.Id, Email = user.Email, Name = user.Name, Role = user.Role };
    }

    public async Task<(UserDto User, string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken)> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.FindSingleAsync(u => u.Email.Equals(loginDto.Email, StringComparison.OrdinalIgnoreCase));
        if (user == null || !_passwordHasher.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new SecurityException("Invalid email or password.");
        }

        var (accessToken, accessTokenExpiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _refreshTokenRepository.SaveChangesAsync();

        return (new UserDto { Id = user.Id, Email = user.Email, Name = user.Name, Role = user.Role }, accessToken, accessTokenExpiresAt, refreshToken);
    }

    public async Task<(UserDto User, string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity == null || tokenEntity.IsUsed || tokenEntity.IsRevoked || tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            throw new SecurityException("Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(tokenEntity.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        tokenEntity.IsUsed = true;
        await _refreshTokenRepository.SaveChangesAsync();

        var (accessToken, accessTokenExpiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        var newTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(newTokenEntity);
        await _refreshTokenRepository.SaveChangesAsync();

        return (new UserDto { Id = user.Id, Email = user.Email, Name = user.Name, Role = user.Role }, accessToken, accessTokenExpiresAt, newRefreshToken);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity != null)
        {
            tokenEntity.IsRevoked = true;
            await _refreshTokenRepository.SaveChangesAsync();
        }
    }
}
