using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Auth;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Services;
using SmartTask.UnitTests.TestSupport;

namespace SmartTask.UnitTests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetAssignableUsersAsync_ExcludesAdminsAndCurrentUser()
    {
        using var db = new TestDatabase();
        var admin = db.AddUser("admin@example.com", Role.Admin);
        var manager = db.AddUser("pm@example.com", Role.ProjectManager);
        db.AddUser("dev@example.com", Role.TeamMember);
        var service = new UserService(db.Users, new PasswordHasher());

        var managers = await service.GetAssignableUsersAsync(Role.ProjectManager, admin.Id);
        var members = await service.GetAssignableUsersAsync(Role.TeamMember, manager.Id);

        Assert.DoesNotContain(managers, u => u.Id == admin.Id);
        Assert.DoesNotContain(managers, u => u.Email == "admin@example.com");
        Assert.Single(managers);
        Assert.Single(members);
        Assert.Equal("dev@example.com", members.Single().Email);
    }

    [Fact]
    public async Task CreateUserAsync_WithAdminRole_ThrowsInvalidOperation()
    {
        using var db = new TestDatabase();
        var service = new UserService(db.Users, new PasswordHasher());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateUserAsync(new CreateUserDto { Email = "new@example.com", Password = "Passw0rd!", Role = Role.Admin }));
    }

    [Fact]
    public async Task CreateUserAsync_PersistsTeamMemberWithHashedPassword()
    {
        using var db = new TestDatabase();
        var service = new UserService(db.Users, new PasswordHasher());

        var created = await service.CreateUserAsync(new CreateUserDto
        {
            Email = "  New@Example.com ",
            Password = "Passw0rd!",
            Name = "New Member",
            Role = Role.TeamMember
        });

        Assert.Equal("new@example.com", created.Email);
        var stored = await db.Context.Users.FindAsync(created.Id);
        Assert.NotNull(stored);
        Assert.NotEqual("Passw0rd!", stored!.PasswordHash);
        Assert.Equal(Role.TeamMember, stored.Role);
    }

    [Fact]
    public async Task UpdateProfileAsync_ChangesNameAndEmail()
    {
        using var db = new TestDatabase();
        var user = db.AddUser("old@example.com", Role.TeamMember, "Old Name");
        var service = new UserService(db.Users, new PasswordHasher());

        var updated = await service.UpdateProfileAsync(user.Id, new UpdateProfileDto
        {
            Email = "new@example.com",
            Name = "New Name"
        });

        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithWrongCurrentPassword_ThrowsInvalidOperation()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var user = db.AddUser("user@example.com", Role.TeamMember);
        var stored = await db.Context.Users.FindAsync(user.Id);
        stored!.PasswordHash = hasher.Hash("OldPassw0rd!");
        await db.Context.SaveChangesAsync();
        var service = new UserService(db.Users, hasher);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateProfileAsync(user.Id, new UpdateProfileDto
            {
                Email = "user@example.com",
                CurrentPassword = "WrongPass!",
                NewPassword = "NewPassw0rd!",
                ConfirmPassword = "NewPassw0rd!"
            }));
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserMissing_ThrowsNotFound()
    {
        using var db = new TestDatabase();
        var service = new UserService(db.Users, new PasswordHasher());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileDto { Email = "ghost@example.com" }));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithMismatchedConfirmation_ThrowsInvalidOperation()
    {
        using var db = new TestDatabase();
        var hasher = new PasswordHasher();
        var user = db.AddUser("mismatch@example.com", Role.TeamMember);
        var stored = await db.Context.Users.FindAsync(user.Id);
        stored!.PasswordHash = hasher.Hash("OldPassw0rd!");
        await db.Context.SaveChangesAsync();
        var service = new UserService(db.Users, hasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateProfileAsync(user.Id, new UpdateProfileDto
            {
                Email = "mismatch@example.com",
                CurrentPassword = "OldPassw0rd!",
                NewPassword = "NewPassw0rd!",
                ConfirmPassword = "OtherPassw0rd!"
            }));

        Assert.Equal("New passwords do not match.", exception.Message);
    }
}