using System.Net;
using System.Net.Http.Json;
using SmartTask.API;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Auth;

[Collection(E2ECollection.Name)]
public class AuthE2ETests
{
    private readonly E2EFixture _fixture;

    public AuthE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_WithNewEmail_CreatesTeamMemberThatCanLogIn()
    {
        var email = $"e2e.register.{Guid.NewGuid():N}@smarttask.dev";
        var response = await _fixture.Api.PostAsync("/api/auth/register", new { Email = email, Password = "Passw0rd!23", Name = "E2E Register" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<UserDto>(response);
        Assert.True(body.Success);
        Assert.Equal(email, body.Data!.Email);
        Assert.Equal(Role.TeamMember, body.Data.Role);

        var session = await _fixture.Api.LoginAsync(email, "Passw0rd!23");
        Assert.Equal(Role.TeamMember, session.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
    }

    [Fact]
    public async Task Register_WithExistingEmail_DoesNotCreateSecondUser()
    {
        var response = await _fixture.Api.PostAsync("/api/auth/register", new
        {
            Email = TestData.AdminEmail,
            Password = "Passw0rd!23",
            Name = "Duplicate Admin"
        });

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(ApiClient.Json);
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Contains("already exists", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsNoTokens()
    {
        var response = await _fixture.Api.PostAsync("/api/auth/login", new { Email = TestData.AdminEmail, Password = "WrongPassword!1" });

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResultDto>>(ApiClient.Json);
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Null(body.Data);
        Assert.Contains("Invalid email or password", body.Message);
    }

    [Fact]
    public async Task Login_WithSeededUsers_ReturnsExpectedRoles()
    {
        Assert.Equal(Role.Admin, _fixture.Admin.User.Role);
        Assert.Equal(Role.ProjectManager, _fixture.Manager.User.Role);
        Assert.Equal(Role.TeamMember, _fixture.Member.User.Role);

        var response = await _fixture.Api.GetAsync("/api/dashboard/stats", _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesToken_AndRejectsReplay()
    {
        var session = await _fixture.Api.LoginAsync(TestData.MemberEmail, TestData.MemberPassword);

        var first = await _fixture.Api.PostAsync("/api/auth/refresh-token", session.RefreshToken, session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await ApiClient.ReadAsync<AuthResultDto>(first);
        Assert.NotEqual(session.RefreshToken, firstBody.Data!.RefreshToken);

        var replay = await _fixture.Api.PostAsync("/api/auth/refresh-token", session.RefreshToken, session.AccessToken);
        Assert.NotEqual(HttpStatusCode.OK, replay.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var session = await _fixture.Api.LoginAsync(TestData.ManagerEmail, TestData.ManagerPassword);

        var logout = await _fixture.Api.PostAsync("/api/auth/logout", session.RefreshToken, session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var refreshAfterLogout = await _fixture.Api.PostAsync("/api/auth/refresh-token", session.RefreshToken, session.AccessToken);
        Assert.NotEqual(HttpStatusCode.OK, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_WithoutToken_Return401()
    {
        var projects = await _fixture.Api.GetAsync("/api/projects/paged");
        var tasks = await _fixture.Api.GetAsync("/api/taskitems/paged");
        var dashboard = await _fixture.Api.GetAsync("/api/dashboard/stats");
        var enhance = await _fixture.Api.PostAsync("/api/tasks/enhance-description", new { Description = "A description long enough for validation." });

        Assert.Equal(HttpStatusCode.Unauthorized, projects.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, tasks.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, enhance.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_WithInvalidToken_Return401()
    {
        var response = await _fixture.Api.GetAsync("/api/projects/paged", "not-a-valid-jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
