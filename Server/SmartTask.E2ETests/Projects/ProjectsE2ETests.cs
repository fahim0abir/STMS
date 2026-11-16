using System.Net;
using System.Net.Http.Json;
using SmartTask.API;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Projects;

[Collection(E2ECollection.Name)]
public class ProjectsE2ETests
{
    private readonly E2EFixture _fixture;

    public ProjectsE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateProject_AsProjectManager_IsVisibleToOwnerOnly()
    {
        var name = $"Gamma E2E Project {Guid.NewGuid():N}";
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = name, Description = "Created by E2E suite", StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Manager.AccessToken);

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        Assert.True(created.Success);
        Assert.Equal(name, created.Data!.Name);
        Assert.Equal(TestData.ManagerEmail, created.Data.ManagerEmail);

        var managerList = await _fixture.Api.GetAsync($"/api/projects/paged?keyword={Uri.EscapeDataString(name)}", _fixture.Manager.AccessToken);
        var managerBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(managerList);
        Assert.Single(managerBody.Data!.Items);

        var adminList = await _fixture.Api.GetAsync($"/api/projects/paged?keyword={Uri.EscapeDataString(name)}", _fixture.Admin.AccessToken);
        var adminBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(adminList);
        Assert.Single(adminBody.Data!.Items);

        var memberList = await _fixture.Api.GetAsync($"/api/projects/paged?keyword={Uri.EscapeDataString(name)}", _fixture.Member.AccessToken);
        var memberBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(memberList);
        Assert.Empty(memberBody.Data!.Items);
    }

    [Fact]
    public async Task CreateProject_AsTeamMember_Returns403()
    {
        var response = await _fixture.Api.PostAsync("/api/projects", new { Name = "Forbidden Project", StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveProject_HidesFromDefaultList_RestoreBringsItBack()
    {
        var name = $"Archivable {Guid.NewGuid():N}";
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = name, StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Admin.AccessToken);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        var projectId = created.Data!.Id;

        var managerArchive = await _fixture.Api.PatchAsync($"/api/projects/{projectId}/archive", new { }, _fixture.Manager.AccessToken);
        Assert.Equal(HttpStatusCode.OK, managerArchive.StatusCode);

        var defaultList = await _fixture.Api.GetAsync($"/api/projects/paged?keyword={Uri.EscapeDataString(name)}", _fixture.Admin.AccessToken);
        var defaultBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(defaultList);
        Assert.Empty(defaultBody.Data!.Items);

        var archivedList = await _fixture.Api.GetAsync($"/api/projects/paged?keyword={Uri.EscapeDataString(name)}&includeArchived=true", _fixture.Admin.AccessToken);
        var archivedBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(archivedList);
        Assert.Single(archivedBody.Data!.Items);
        Assert.Equal(ProjectStatus.Archived, archivedBody.Data.Items[0].Status);

        var restore = await _fixture.Api.PatchAsync($"/api/projects/{projectId}/unarchive", new { }, _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);

        var afterRestore = await _fixture.Api.GetAsync($"/api/projects/{projectId}", _fixture.Admin.AccessToken);
        var restoredBody = await ApiClient.ReadAsync<ProjectDto>(afterRestore);
        Assert.Equal(ProjectStatus.Active, restoredBody.Data!.Status);
    }

    [Fact]
    public async Task UpdateProject_WithValidPayload_PersistsChanges()
    {
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = $"Updatable {Guid.NewGuid():N}", StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Admin.AccessToken);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        var projectId = created.Data!.Id;

        var updatedName = $"Updated {Guid.NewGuid():N}";
        var update = await _fixture.Api.PutAsync($"/api/projects/{projectId}", new { Name = updatedName, Description = "Updated by E2E", StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var fetched = await _fixture.Api.GetAsync($"/api/projects/{projectId}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<ProjectDto>(fetched);
        Assert.Equal(updatedName, body.Data!.Name);
        Assert.Equal("Updated by E2E", body.Data.Description);
    }

    [Fact]
    public async Task AssignProjectManager_AsAdmin_ReassignsManager_AsManager_Returns403()
    {
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = $"Assignable {Guid.NewGuid():N}", StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Admin.AccessToken);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        var projectId = created.Data!.Id;

        var managerAttempt = await _fixture.Api.PatchAsync(
            $"/api/projects/{projectId}/manager",
            new { ManagerId = TestData.SecondManagerId },
            _fixture.Manager.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, managerAttempt.StatusCode);

        var adminAssign = await _fixture.Api.PatchAsync(
            $"/api/projects/{projectId}/manager",
            new { ManagerId = TestData.SecondManagerId },
            _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, adminAssign.StatusCode);

        var fetched = await _fixture.Api.GetAsync($"/api/projects/{projectId}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<ProjectDto>(fetched);
        Assert.Equal(TestData.SecondManagerId, body.Data!.ManagerId);
        Assert.Equal(TestData.SecondManagerEmail, body.Data.ManagerEmail);
    }

    [Fact]
    public async Task GetProject_WithUnknownId_Returns404()
    {
        var response = await _fixture.Api.GetAsync($"/api/projects/{Guid.NewGuid()}", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PagedProjects_SupportKeywordSortingAndPagination()
    {
        var adminAll = await _fixture.Api.GetAsync("/api/projects/paged?pageSize=100&sortBy=Name&sortOrder=desc", _fixture.Admin.AccessToken);
        var adminBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(adminAll);
        Assert.Equal(HttpStatusCode.OK, adminAll.StatusCode);
        Assert.True(adminBody.Data!.TotalCount >= 2);

        var names = adminBody.Data.Items.Select(p => p.Name).ToList();
        var sortedDescending = names.OrderByDescending(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(sortedDescending, names);

        var keyword = await _fixture.Api.GetAsync("/api/projects/paged?keyword=Alpha", _fixture.Admin.AccessToken);
        var keywordBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(keyword);
        Assert.All(keywordBody.Data!.Items, p => Assert.Contains("Alpha", p.Name, StringComparison.OrdinalIgnoreCase));

        var firstPage = await _fixture.Api.GetAsync("/api/projects/paged?pageNumber=1&pageSize=1", _fixture.Admin.AccessToken);
        var firstPageBody = await ApiClient.ReadAsync<PagedResult<ProjectDto>>(firstPage);
        Assert.Single(firstPageBody.Data!.Items);
        Assert.Equal(1, firstPageBody.Data.PageNumber);
        Assert.Equal(1, firstPageBody.Data.PageSize);
        Assert.True(firstPageBody.Data.TotalPages >= 2);
    }
}
