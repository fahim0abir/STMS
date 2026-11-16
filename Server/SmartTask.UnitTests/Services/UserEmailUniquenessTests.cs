using Microsoft.EntityFrameworkCore;
using Moq;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Auth;
using SmartTask.Infrastructure.Services;
using SmartTask.UnitTests.TestSupport;

namespace SmartTask.UnitTests.Services;

public class UserEmailUniquenessTests
{
    [Fact]
    public void UserEmail_HasUniqueIndex()
    {
        using var db = new TestDatabase();

        var index = db.Context.Model
            .FindEntityType(typeof(User))!
            .GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(User.Email));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task CreateUserAsync_WithCaseVariantDuplicateEmail_Throws()
    {
        using var db = new TestDatabase();
        db.AddUser("taken@example.com", Role.TeamMember);
        var service = new UserService(db.Users, new PasswordHasher());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(new CreateUserDto
        {
            Email = "  Taken@Example.COM ",
            Password = "Passw0rd!",
            Role = Role.ProjectManager
        }));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(db.Context.Users.Where(u => u.Email == "taken@example.com"));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithDuplicateEmail_Throws()
    {
        using var db = new TestDatabase();
        db.AddUser("taken@example.com", Role.TeamMember);
        var user = db.AddUser("me@example.com", Role.TeamMember);
        var service = new UserService(db.Users, new PasswordHasher());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateProfileAsync(user.Id, new UpdateProfileDto
        {
            Email = "TAKEN@example.com",
            Name = "Me"
        }));
    }

    [Fact]
    public async Task IsEmailAvailableAsync_NormalizesEmailBeforeLookup()
    {
        using var db = new TestDatabase();
        db.AddUser("taken@example.com", Role.TeamMember);
        var service = new AuthService(
            db.Users,
            db.RefreshTokens,
            new Mock<IPasswordHasher>().Object,
            new Mock<IJwtTokenService>().Object);

        Assert.False(await service.IsEmailAvailableAsync("  TAKEN@Example.com "));
        Assert.True(await service.IsEmailAvailableAsync("free@example.com"));
        Assert.True(await service.IsEmailAvailableAsync("   "));
    }
}
