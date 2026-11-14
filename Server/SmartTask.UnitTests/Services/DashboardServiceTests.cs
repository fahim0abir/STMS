using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Services;
using SmartTask.UnitTests.TestSupport;
using TaskPriority = SmartTask.Domain.Enums.TaskPriority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.UnitTests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetStatsAsync_OnEmptyDatabase_ReturnsZeros()
    {
        using var db = new TestDatabase();
        var service = new DashboardService(db.Projects, db.Tasks);

        var stats = await service.GetStatsAsync(Guid.NewGuid(), "Admin");

        Assert.Equal(0, stats.TotalProjects);
        Assert.Equal(0, stats.TotalTasks);
        Assert.Empty(stats.TasksByStatus);
        Assert.Empty(stats.TasksByPriority);
    }

    [Fact]
    public async Task GetStatsAsync_AggregatesCountsByStatusAndPriority()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        db.AddProject(owner.Id, "One");
        db.AddProject(owner.Id, "Two");
        var project = db.Context.Projects.First();

        db.AddTask(project.Id, "Done", TaskStatus.Completed, TaskPriority.High);
        db.AddTask(project.Id, "Wip", TaskStatus.InProgress, TaskPriority.Low);
        db.AddTask(project.Id, "Todo", TaskStatus.ToDo, TaskPriority.Low);
        db.AddTask(project.Id, "Cancelled", TaskStatus.Cancelled, TaskPriority.Critical);

        var stats = await new DashboardService(db.Projects, db.Tasks).GetStatsAsync(owner.Id, "Admin");

        Assert.Equal(2, stats.TotalProjects);
        Assert.Equal(4, stats.TotalTasks);
        Assert.Equal(1, stats.CompletedTasks);
        Assert.Equal(3, stats.PendingTasks);
        Assert.Equal(2, stats.TasksByPriority["Low"]);
        Assert.Equal(1, stats.TasksByPriority["High"]);
        Assert.Equal(1, stats.TasksByStatus["Completed"]);
        Assert.Equal(1, stats.TasksByStatus["InProgress"]);
    }

    [Fact]
    public async Task GetStatsAsync_CountsOnlyFutureDueIncompleteTasks()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        db.AddTask(project.Id, "Overdue", TaskStatus.ToDo, dueAt: DateTime.UtcNow.AddDays(-2));
        db.AddTask(project.Id, "Upcoming", TaskStatus.ToDo, dueAt: DateTime.UtcNow.AddDays(2));
        db.AddTask(project.Id, "Future but done", TaskStatus.Completed, dueAt: DateTime.UtcNow.AddDays(3));
        db.AddTask(project.Id, "No due date", TaskStatus.ToDo);

        var stats = await new DashboardService(db.Projects, db.Tasks).GetStatsAsync(owner.Id, "Admin");

        Assert.Equal(1, stats.UpcomingDueTasks);
    }

    [Fact]
    public async Task GetStatsAsync_ScopesDataByRole()
    {
        using var db = new TestDatabase();
        var manager = db.AddUser("pm@example.com", Role.ProjectManager);
        var otherManager = db.AddUser("other@example.com", Role.ProjectManager);
        var member = db.AddUser("dev@example.com", Role.TeamMember);
        var managedProject = db.AddProject(manager.Id, "Managed");
        var foreignProject = db.AddProject(otherManager.Id, "Foreign");
        db.AddTask(managedProject.Id, "Mine", TaskStatus.ToDo, TaskPriority.High, member.Id);
        db.AddTask(foreignProject.Id, "Other", TaskStatus.ToDo, TaskPriority.Low, member.Id);
        var service = new DashboardService(db.Projects, db.Tasks);

        var managerStats = await service.GetStatsAsync(manager.Id, "ProjectManager");
        Assert.Equal(1, managerStats.TotalProjects);
        Assert.Equal(1, managerStats.TotalTasks);

        var memberStats = await service.GetStatsAsync(member.Id, "TeamMember");
        Assert.Equal(2, memberStats.TotalProjects);
        Assert.Equal(2, memberStats.TotalTasks);

        var adminStats = await service.GetStatsAsync(Guid.NewGuid(), "Admin");
        Assert.Equal(2, adminStats.TotalProjects);
        Assert.Equal(2, adminStats.TotalTasks);
    }
}
