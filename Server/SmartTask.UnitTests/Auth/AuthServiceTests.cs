using Moq;
using SmartTask.Application.DTOs.Auth;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Auth;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.UnitTests.TestSupport;

namespace SmartTask.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly DateTime _expiresAt = DateTime.UtcNow.AddMinutes(30);

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hashed::{p}");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string password, string hash) => hash == $"hashed::{password}");
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns(("access-token", _expiresAt));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
    }

    private AuthService CreateService(TestDatabase db) =>
        new(db.Users, db.RefreshTokens, _hasher.Object, _jwt.Object);

    [Fact]
    public async Task RegisterAsync_CreatesTeamMemberWithLowercasedEmail()
    {
        using var db = new TestDatabase();

        var dto = await CreateService(db).RegisterAsync(new RegisterDto
        {
            Email = "User@Example.COM",
            Password = "Password123",
            Name = "Jane"
        });

        Assert.Equal("user@example.com", dto.Email);
        Assert.Equal(Role.TeamMember, dto.Role);
        Assert.Equal("hashed::Password123", db.Context.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperation()
    {
        using var db = new TestDatabase();
        db.AddUser("taken@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(db).RegisterAsync(new RegisterDto
        {
            Email = "TAKEN@example.com",
            Password = "Password123"
        }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndStoresRefreshToken()
    {
        using var db = new TestDatabase();
        var user = db.AddUser("user@example.com");
        db.Context.Users.Single().PasswordHash = "hashed::Password123";
        db.Context.SaveChanges();

        var result = await CreateService(db).LoginAsync(new LoginDto
        {
            Email = "USER@example.com",
            Password = "Password123"
        });

        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(_expiresAt, result.AccessTokenExpiresAt);
        Assert.False(db.Context.RefreshTokens.Single().IsUsed);
    }

    [Theory]
    [InlineData("user@example.com", "wrong-password")]
    [InlineData("missing@example.com", "Password123")]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsSecurityException(string email, string password)
    {
        using var db = new TestDatabase();
        db.AddUser("user@example.com");
        db.Context.Users.Single().PasswordHash = "hashed::Password123";
        db.Context.SaveChanges();

        await Assert.ThrowsAsync<SecurityException>(() =>
            CreateService(db).LoginAsync(new LoginDto { Email = email, Password = password }));
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesRefreshTokenAndMarksOldOneUsed()
    {
        using var db = new TestDatabase();
        var user = db.AddUser("user@example.com");
        var existing = db.AddRefreshToken(user.Id, "old-token", DateTime.UtcNow.AddDays(1));

        var result = await CreateService(db).RefreshTokenAsync("old-token");

        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.True(db.Context.RefreshTokens.Single(t => t.Id == existing.Id).IsUsed);
        Assert.Equal(2, db.Context.RefreshTokens.Count());
    }

    [Fact]
    public async Task RefreshTokenAsync_WithUnknownToken_ThrowsSecurityException()
    {
        using var db = new TestDatabase();

        await Assert.ThrowsAsync<SecurityException>(() => CreateService(db).RefreshTokenAsync("nope"));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithUsedToken_ThrowsSecurityException()
    {
        using var db = new TestDatabase();
        var user = db.AddUser("user@example.com");
        db.AddRefreshToken(user.Id, "used-token", DateTime.UtcNow.AddDays(1), isUsed: true);

        await Assert.ThrowsAsync<SecurityException>(() => CreateService(db).RefreshTokenAsync("used-token"));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsSecurityException()
    {
        using var db = new TestDatabase();
        var user = db.AddUser("user@example.com");
        db.AddRefreshToken(user.Id, "expired-token", DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<SecurityException>(() => CreateService(db).RefreshTokenAsync("expired-token"));
    }

    [Fact]
    public async Task LogoutAsync_RevokesToken()
    {
        using var db = new TestDatabase();
        var user = db.AddUser("user@example.com");
        var token = db.AddRefreshToken(user.Id, "live-token", DateTime.UtcNow.AddDays(1));

        await CreateService(db).LogoutAsync("live-token");

        Assert.True(db.Context.RefreshTokens.Single(t => t.Id == token.Id).IsRevoked);
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownToken_DoesNotThrow()
    {
        using var db = new TestDatabase();

        await CreateService(db).LogoutAsync("missing-token");
    }
}
