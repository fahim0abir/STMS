using System.Net;
using SmartTask.Application.DTOs;
using SmartTask.E2ETests.Infrastructure;
using TaskPriority = SmartTask.Domain.Enums.TaskPriority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.E2ETests.Tasks;

[Collection(E2ECollection.Name)]
public class TasksE2ETests
{
    private readonly E2EFixture _fixture;

    public TasksE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<TaskItemDto> CreateTaskAsync(TaskStatus status, TaskPriority priority, Guid? assigneeId = null, string? title = null)
    {
        var payload = new
        {
            Title = title ?? $"E2E task {Guid.NewGuid():N}",
            Description = "Created by the E2E suite",
            Status = status,
            Priority = priority,
            AssigneeId = assigneeId,
            DueAt = DateTime.UtcNow.AddDays(2)
        };

        using var response = await _fixture.Api.PostAsync($"/api/taskitems?projectId={TestData.AdminProjectId}", payload, _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<TaskItemDto>(response);
        return body.Data!;
    }

    [Fact]
    public async Task CreateTask_AsAdmin_IsReturnedByProjectAndPagedEndpoints()
    {
        var created = await CreateTaskAsync(TaskStatus.ToDo, TaskPriority.High, TestData.MemberId);

        var byId = await _fixture.Api.GetAsync($"/api/taskitems/{created.Id}", _fixture.Admin.AccessToken);
        var byIdBody = await ApiClient.ReadAsync<TaskItemDto>(byId);
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(created.Title, byIdBody.Data!.Title);
        Assert.Equal(TestData.MemberId, byIdBody.Data.AssigneeId);

        var byProject = await _fixture.Api.GetAsync($"/api/taskitems?projectId={TestData.AdminProjectId}", _fixture.Admin.AccessToken);
        var byProjectBody = await ApiClient.ReadAsync<IEnumerable<TaskItemDto>>(byProject);
        Assert.Contains(byProjectBody.Data!, t => t.Id == created.Id);

        var paged = await _fixture.Api.GetAsync($"/api/taskitems/paged?keyword={Uri.EscapeDataString(created.Title)}", _fixture.Admin.AccessToken);
        var pagedBody = await ApiClient.ReadAsync<PagedResult<TaskItemDto>>(paged);
        Assert.Single(pagedBody.Data!.Items);
        Assert.Equal(created.Id, pagedBody.Data.Items[0].Id);
    }

    [Fact]
    public async Task CreateTask_AsTeamMember_Returns403()
    {
        var payload = new { Title = "Forbidden task", Status = TaskStatus.ToDo, Priority = TaskPriority.Low };
        var response = await _fixture.Api.PostAsync($"/api/taskitems?projectId={TestData.AdminProjectId}", payload, _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatus_AsAssignee_Succeeds()
    {
        var created = await CreateTaskAsync(TaskStatus.ToDo, TaskPriority.Medium, TestData.MemberId);

        var patch = await _fixture.Api.PatchAsync($"/api/taskitems/{created.Id}/status", new { Status = TaskStatus.InProgress }, _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var fetched = await _fixture.Api.GetAsync($"/api/taskitems/{created.Id}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<TaskItemDto>(fetched);
        Assert.Equal(TaskStatus.InProgress, body.Data!.Status);
    }

    [Fact]
    public async Task UpdateTaskStatus_AsUnrelatedTeamMember_Returns403()
    {
        var response = await _fixture.Api.PatchAsync(
            $"/api/taskitems/{TestData.AdminAssignedTaskId}/status",
            new { Status = TaskStatus.Completed },
            _fixture.Member.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatus_WithUnknownStatus_Returns400()
    {
        var response = await _fixture.Api.PatchAsync(
            $"/api/taskitems/{TestData.MemberAssignedTaskId}/status",
            new { Status = 99 },
            _fixture.Admin.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_AsAdmin_PersistsTitleAndPriority()
    {
        var created = await CreateTaskAsync(TaskStatus.ToDo, TaskPriority.Low);

        var update = await _fixture.Api.PutAsync(
            $"/api/taskitems/{created.Id}",
            new
            {
                Title = "Updated E2E task title",
                Description = "Updated",
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Critical,
                AssigneeId = TestData.MemberId,
                DueAt = DateTime.UtcNow.AddDays(7)
            },
            _fixture.Admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var fetched = await _fixture.Api.GetAsync($"/api/taskitems/{created.Id}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<TaskItemDto>(fetched);
        Assert.Equal("Updated E2E task title", body.Data!.Title);
        Assert.Equal(TaskPriority.Critical, body.Data.Priority);
        Assert.Equal(TaskStatus.InProgress, body.Data.Status);
    }

    [Fact]
    public async Task PagedTasks_FilterByStatusPriorityAndAssignee()
    {
        var title = $"Filtered task {Guid.NewGuid():N}";
        await CreateTaskAsync(TaskStatus.Completed, TaskPriority.Critical, TestData.MemberId, title);

        var filtered = await _fixture.Api.GetAsync(
            $"/api/taskitems/paged?keyword={Uri.EscapeDataString("Filtered task")}&status={TaskStatus.Completed}&priority={TaskPriority.Critical}&assigneeId={TestData.MemberId}&pageSize=100",
            _fixture.Admin.AccessToken);

        var body = await ApiClient.ReadAsync<PagedResult<TaskItemDto>>(filtered);
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        Assert.Contains(body.Data!.Items, t => t.Title == title);
        Assert.All(body.Data.Items, t =>
        {
            Assert.Equal(TaskStatus.Completed, t.Status);
            Assert.Equal(TaskPriority.Critical, t.Priority);
            Assert.Equal(TestData.MemberId, t.AssigneeId);
        });
    }

    [Fact]
    public async Task PagedTasks_AsTeamMember_OnlyReturnsAssignedTasks()
    {
        var response = await _fixture.Api.GetAsync("/api/taskitems/paged?pageSize=100", _fixture.Member.AccessToken);
        var body = await ApiClient.ReadAsync<PagedResult<TaskItemDto>>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(body.Data!.Items);
        Assert.All(body.Data.Items, t => Assert.Equal(TestData.MemberId, t.AssigneeId));
    }

    [Fact]
    public async Task DeleteTask_AsAdmin_RemovesTask_AsTeamMember_Returns403()
    {
        var created = await CreateTaskAsync(TaskStatus.ToDo, TaskPriority.Low);

        var memberDelete = await _fixture.Api.DeleteAsync($"/api/taskitems/{created.Id}", _fixture.Member.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, memberDelete.StatusCode);

        var adminDelete = await _fixture.Api.DeleteAsync($"/api/taskitems/{created.Id}", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, adminDelete.StatusCode);

        var afterDelete = await _fixture.Api.GetAsync($"/api/taskitems/{created.Id}", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task AssignTask_AsAdmin_UpdatesAssignee()
    {
        var created = await CreateTaskAsync(TaskStatus.ToDo, TaskPriority.Medium);

        var patch = await _fixture.Api.PatchAsync(
            $"/api/taskitems/{created.Id}/assignee",
            new { AssigneeId = TestData.SecondMemberId },
            _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var fetched = await _fixture.Api.GetAsync($"/api/taskitems/{created.Id}", _fixture.Admin.AccessToken);
        var body = await ApiClient.ReadAsync<TaskItemDto>(fetched);
        Assert.Equal(TestData.SecondMemberId, body.Data!.AssigneeId);
        Assert.Equal(TestData.SecondMemberEmail, body.Data.AssigneeEmail);
    }

    [Fact]
    public async Task AssignTask_AsProjectManager_OnForeignProject_Returns403()
    {
        var create = await _fixture.Api.PostAsync("/api/projects", new { Name = $"Foreign {Guid.NewGuid():N}", StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" }, _fixture.Admin.AccessToken);
        var project = await ApiClient.ReadAsync<ProjectDto>(create);
        var payload = new { Title = "Foreign task", Status = TaskStatus.ToDo, Priority = TaskPriority.Low };
        var task = await _fixture.Api.PostAsync($"/api/taskitems?projectId={project.Data!.Id}", payload, _fixture.Admin.AccessToken);
        var taskBody = await ApiClient.ReadAsync<TaskItemDto>(task);

        var patch = await _fixture.Api.PatchAsync(
            $"/api/taskitems/{taskBody.Data!.Id}/assignee",
            new { AssigneeId = TestData.MemberId },
            _fixture.Manager.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
    }

    [Fact]
    public async Task AssignTask_AsProjectManager_OnOwnProject_Succeeds()
    {
        var create = await _fixture.Api.PostAsync(
            "/api/projects",
            new { Name = $"Owned {Guid.NewGuid():N}", ManagerId = TestData.ManagerId, StartDate = DateTime.UtcNow, TechStack = "ASP.NET Core 9" },
            _fixture.Admin.AccessToken);
        var project = await ApiClient.ReadAsync<ProjectDto>(create);
        var payload = new { Title = "Owned task", Status = TaskStatus.ToDo, Priority = TaskPriority.Low };
        var task = await _fixture.Api.PostAsync($"/api/taskitems?projectId={project.Data!.Id}", payload, _fixture.Manager.AccessToken);
        var taskBody = await ApiClient.ReadAsync<TaskItemDto>(task);

        var patch = await _fixture.Api.PatchAsync(
            $"/api/taskitems/{taskBody.Data!.Id}/assignee",
            new { AssigneeId = TestData.MemberId },
            _fixture.Manager.AccessToken);

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
    }

    [Fact]
    public async Task GetTask_WithUnknownId_Returns404()
    {
        var response = await _fixture.Api.GetAsync($"/api/taskitems/{Guid.NewGuid()}", _fixture.Admin.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
