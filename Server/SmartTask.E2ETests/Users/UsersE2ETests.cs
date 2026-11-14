using System.Net;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Users;

[Collection(E2ECollection.Name)]
public class UsersE2ETests
{
    private readonly E2EFixture _fixture;

    public UsersE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsers_ExcludesAdminsAndCaller()
    {
        var response = await _fixture.Api.GetAsync("/api/users", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ApiClient.ReadAsync<IEnumerable<UserDto>>(response);
        Assert.DoesNotContain(body.Data!, u => u.Role == Role.Admin);
        Assert.DoesNotContain(body.Data!, u => u.Id == TestData.AdminId);
    }

    [Fact]
    public async Task GetUsers_AsTeamMember_Returns403()
    {
        var response = await _fixture.Api.GetAsync("/api/users", _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithTeamMemberRoleFilter_ReturnsOnlyTeamMembers()
    {
        var response = await _fixture.Api.GetAsync($"/api/users?role={(int)Role.TeamMember}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<IEnumerable<UserDto>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(body.Data!);
        Assert.All(body.Data!, u => Assert.Equal(Role.TeamMember, u.Role));
    }

    [Fact]
    public async Task CreateUser_AsAdmin_CreatesTeamMember()
    {
        var email = $"e2e-{Guid.NewGuid():N}@smarttask.dev";
        var response = await _fixture.Api.PostAsync(
            "/api/users",
            new { Email = email, Password = "Passw0rd!", Name = "E2E User", Role = Role.TeamMember },
            _fixture.Admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<UserDto>(response);
        Assert.Equal(email, body.Data!.Email);
        Assert.Equal(Role.TeamMember, body.Data.Role);
    }

    [Fact]
    public async Task CreateUser_WithAdminRole_Returns400()
    {
        var response = await _fixture.Api.PostAsync(
            "/api/users",
            new { Email = $"e2e-admin-{Guid.NewGuid():N}@smarttask.dev", Password = "Passw0rd!", Role = Role.Admin },
            _fixture.Admin.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsProjectManager_Returns403()
    {
        var response = await _fixture.Api.PostAsync(
            "/api/users",
            new { Email = $"e2e-pm-{Guid.NewGuid():N}@smarttask.dev", Password = "Passw0rd!", Role = Role.TeamMember },
            _fixture.Manager.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_UpdatesOwnName()
    {
        var response = await _fixture.Api.PutAsync(
            "/api/users/me",
            new { Email = TestData.MemberEmail, Name = "Updated Member" },
            _fixture.Member.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<UserDto>(response);
        Assert.Equal("Updated Member", body.Data!.Name);
        Assert.Equal(TestData.MemberEmail, body.Data.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithMismatchedConfirmation_Returns400()
    {
        var response = await _fixture.Api.PutAsync(
            "/api/users/me",
            new
            {
                Email = TestData.MemberEmail,
                CurrentPassword = TestData.MemberPassword,
                NewPassword = "BrandNewPassw0rd!",
                ConfirmPassword = "DifferentPassw0rd!"
            },
            _fixture.Member.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}