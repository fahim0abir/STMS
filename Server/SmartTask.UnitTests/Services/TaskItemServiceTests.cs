using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Services;
using SmartTask.UnitTests.TestSupport;
using TaskPriority = SmartTask.Domain.Enums.TaskPriority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.UnitTests.Services;

public class TaskItemServiceTests
{
    private static TaskItemService CreateService(TestDatabase db) =>
        new(db.Tasks, db.Projects, db.Users);

    [Fact]
    public async Task CreateAsync_PersistsTaskWithRequestedValues()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        var due = DateTime.UtcNow.AddDays(3);

        var dto = await CreateService(db).CreateAsync(project.Id, new TaskItemCreateDto
        {
            Title = "Ship it",
            Description = "Release the feature",
            Status = TaskStatus.InProgress,
            Priority = TaskPriority.Critical,
            AssigneeId = owner.Id,
            DueAt = due
        });

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal(project.Id, dto.ProjectId);
        Assert.Equal(TaskStatus.InProgress, dto.Status);
        Assert.Equal(TaskPriority.Critical, dto.Priority);
        Assert.Equal(due, dto.DueAt);
        Assert.Single(db.Context.TaskItems);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        using var db = new TestDatabase();

        Assert.Null(await CreateService(db).GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAssigneeEmail()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser("owner@example.com");
        var assignee = db.AddUser("dev@example.com", Role.TeamMember);
        var project = db.AddProject(owner.Id);
        var task = db.AddTask(project.Id, assigneeId: assignee.Id);

        var dto = await CreateService(db).GetByIdAsync(task.Id);

        Assert.NotNull(dto);
        Assert.Equal("dev@example.com", dto!.AssigneeEmail);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        using var db = new TestDatabase();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(db).UpdateAsync(Guid.NewGuid(), new TaskItemCreateDto { Title = "Ghost" }));
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesOnlyStatusAndTimestamp()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        var task = db.AddTask(project.Id, "Keep title");
        var service = CreateService(db);

        await service.UpdateStatusAsync(task.Id, TaskStatus.Completed);

        var stored = await db.Context.TaskItems.FindAsync(task.Id);
        Assert.Equal(TaskStatus.Completed, stored!.Status);
        Assert.Equal("Keep title", stored.Title);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenMissing_ThrowsNotFound()
    {
        using var db = new TestDatabase();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(db).UpdateStatusAsync(Guid.NewGuid(), TaskStatus.Completed));
    }

    [Fact]
    public async Task GetPagedAsync_AsTeamMember_OnlyReturnsAssignedTasks()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser("owner@example.com", Role.ProjectManager);
        var member = db.AddUser("member@example.com", Role.TeamMember);
        var project = db.AddProject(owner.Id);
        db.AddTask(project.Id, "Mine", assigneeId: member.Id);
        db.AddTask(project.Id, "Someone else", assigneeId: owner.Id);
        var service = CreateService(db);

        var result = await service.GetPagedAsync(new TaskItemQueryDto(), member.Id, "TeamMember");

        Assert.Single(result.Items);
        Assert.Equal("Mine", result.Items[0].Title);
    }

    [Fact]
    public async Task GetPagedAsync_AsProjectManager_OnlyReturnsTasksOfOwnedProjects()
    {
        using var db = new TestDatabase();
        var pm = db.AddUser("pm@example.com", Role.ProjectManager);
        var otherPm = db.AddUser("other@example.com", Role.ProjectManager);
        var ownedProject = db.AddProject(pm.Id, "Owned");
        var foreignProject = db.AddProject(otherPm.Id, "Foreign");
        db.AddTask(ownedProject.Id, "Inside");
        db.AddTask(foreignProject.Id, "Outside");
        var service = CreateService(db);

        var result = await service.GetPagedAsync(new TaskItemQueryDto(), pm.Id, "ProjectManager");

        Assert.Single(result.Items);
        Assert.Equal("Inside", result.Items[0].Title);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesFiltersKeywordAndAssignee()
    {
        using var db = new TestDatabase();
        var admin = db.AddUser("admin@example.com", Role.Admin);
        var assignee = db.AddUser("dev@example.com", Role.TeamMember);
        var project = db.AddProject(admin.Id);
        db.AddTask(project.Id, "Fix login bug", TaskStatus.ToDo, TaskPriority.High, assignee.Id);
        db.AddTask(project.Id, "Fix logout bug", TaskStatus.Completed, TaskPriority.High, assignee.Id);
        db.AddTask(project.Id, "Write docs", TaskStatus.ToDo, TaskPriority.Low, admin.Id);
        var service = CreateService(db);

        var result = await service.GetPagedAsync(
            new TaskItemQueryDto
            {
                Keyword = "login",
                Status = TaskStatus.ToDo,
                Priority = TaskPriority.High,
                AssigneeId = assignee.Id
            },
            admin.Id,
            "Admin");

        Assert.Single(result.Items);
        Assert.Equal("Fix login bug", result.Items[0].Title);
        Assert.Equal("dev@example.com", result.Items[0].AssigneeEmail);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesDueDateRangeAndSorting()
    {
        using var db = new TestDatabase();
        var admin = db.AddUser("admin@example.com", Role.Admin);
        var project = db.AddProject(admin.Id);
        var today = DateTime.UtcNow.Date;
        db.AddTask(project.Id, "Later", dueAt: today.AddDays(5));
        db.AddTask(project.Id, "Sooner", dueAt: today.AddDays(1));
        db.AddTask(project.Id, "No due date");
        db.AddTask(project.Id, "Outside range", dueAt: today.AddDays(30));
        var service = CreateService(db);

        var result = await service.GetPagedAsync(
            new TaskItemQueryDto
            {
                DueFrom = today,
                DueTo = today.AddDays(7),
                SortBy = "DueAt",
                SortOrder = "asc"
            },
            admin.Id,
            "Admin");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Sooner", result.Items[0].Title);
        Assert.Equal("Later", result.Items[1].Title);
    }
}
