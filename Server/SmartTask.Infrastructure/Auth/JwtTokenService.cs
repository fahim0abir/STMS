using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartTask.Domain.Entities;

namespace SmartTask.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private byte[]? _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private byte[] GetSigningKey()
    {
        if (_signingKey != null)
        {
            return _signingKey;
        }

        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        _signingKey = Encoding.UTF8.GetBytes(key);
        return _signingKey;
    }

    public (string AccessToken, DateTime AccessTokenExpiresAt) GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(GetSigningKey());
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (accessToken, AccessTokenExpiresAt: token.ValidTo);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
