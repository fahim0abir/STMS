using System.Net;
using SmartTask.Application.DTOs;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Dashboard;

[Collection(E2ECollection.Name)]
public class DashboardE2ETests
{
    private readonly E2EFixture _fixture;

    public DashboardE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetStats_AsTeamMember_ReturnsOnlyAssignedTasks()
    {
        var response = await _fixture.Api.GetAsync("/api/dashboard/stats", _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ApiClient.ReadAsync<DashboardStatsDto>(response);
        var stats = body.Data!;

        Assert.True(body.Success);
        Assert.Equal("Tasks assigned to you", stats.Scope);
        Assert.True(stats.TotalProjects >= 1);
        Assert.True(stats.TotalTasks >= 1);
        Assert.Equal(stats.TotalTasks, stats.CompletedTasks + stats.PendingTasks);
        Assert.Equal(stats.CompletedTasks, stats.TasksByStatus.GetValueOrDefault("Completed"));
        Assert.Equal(stats.TotalTasks, stats.TasksByStatus.Values.Sum());
        Assert.Equal(stats.TotalTasks, stats.TasksByPriority.Values.Sum());
    }

    [Fact]
    public async Task GetStats_AsAdmin_ReturnsGlobalScope()
    {
        var response = await _fixture.Api.GetAsync("/api/dashboard/stats", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<DashboardStatsDto>(response);
        var stats = body.Data!;

        Assert.Equal("All projects and tasks", stats.Scope);
        Assert.True(stats.TotalProjects >= 2);
        Assert.True(stats.TotalTasks >= 4);
    }
}
