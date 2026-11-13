using Microsoft.EntityFrameworkCore;
using SmartTask.Infrastructure.Repositories;
using SmartTask.UnitTests.TestSupport;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.UnitTests.Repositories;

public class QueryablePagingExtensionsTests
{
    [Fact]
    public async Task ToPagedResultAsync_ReturnsRequestedPage()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        for (var i = 1; i <= 5; i++)
        {
            db.AddTask(project.Id, $"Task {i}", createdAt: DateTime.UtcNow.AddMinutes(i));
        }
        var query = db.Tasks.AsQueryable().OrderBy(t => t.Title);

        var result = await query.ToPagedResultAsync(pageNumber: 2, pageSize: 2);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(new[] { "Task 3", "Task 4" }, result.Items.Select(t => t.Title));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-3, 2)]
    public async Task ToPagedResultAsync_ClampsInvalidPageNumber(int pageNumber, int pageSize)
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        db.AddTask(project.Id);
        db.AddTask(project.Id);
        db.AddTask(project.Id);

        var result = await db.Tasks.AsQueryable().ToPagedResultAsync(pageNumber, pageSize);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ToPagedResultAsync_ClampsPageSizeBelowOne()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        db.AddTask(project.Id, "Only", TaskStatus.ToDo);

        var result = await db.Tasks.AsQueryable().ToPagedResultAsync(pageNumber: 1, pageSize: 0);

        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ToPagedResultAsync_OnEmptyQuery_ReturnsZeroPages()
    {
        using var db = new TestDatabase();

        var result = await db.Tasks.AsQueryable().ToPagedResultAsync(1, 10);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }
}
