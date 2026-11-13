using System.Net;
using System.Net.Http.Json;
using SmartTask.API;
using SmartTask.Application.DTOs;
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
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = name, Description = "Created by E2E suite" }, _fixture.Manager.AccessToken);

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        Assert.True(created.Success);
        Assert.Equal(name, created.Data!.Name);
        Assert.Equal(TestData.ManagerEmail, created.Data.OwnerEmail);

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
        var response = await _fixture.Api.PostAsync("/api/projects", new { Name = "Forbidden Project" }, _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_AsProjectManager_Returns403_AsAdmin_Deletes()
    {
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = $"Deletable {Guid.NewGuid():N}" }, _fixture.Admin.AccessToken);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        var projectId = created.Data!.Id;

        var managerDelete = await _fixture.Api.DeleteAsync($"/api/projects/{projectId}", _fixture.Manager.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, managerDelete.StatusCode);

        var adminDelete = await _fixture.Api.DeleteAsync($"/api/projects/{projectId}", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, adminDelete.StatusCode);

        var afterDelete = await _fixture.Api.GetAsync($"/api/projects/{projectId}", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithValidPayload_PersistsChanges()
    {
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = $"Updatable {Guid.NewGuid():N}" }, _fixture.Admin.AccessToken);
        var created = await ApiClient.ReadAsync<ProjectDto>(create);
        var projectId = created.Data!.Id;

        var updatedName = $"Updated {Guid.NewGuid():N}";
        var update = await _fixture.Api.PutAsync($"/api/projects/{projectId}", new { Name = updatedName, Description = "Updated by E2E" }, _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var fetched = await _fixture.Api.GetAsync($"/api/projects/{projectId}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<ProjectDto>(fetched);
        Assert.Equal(updatedName, body.Data!.Name);
        Assert.Equal("Updated by E2E", body.Data.Description);
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
