using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Services;
using SmartTask.UnitTests.TestSupport;

namespace SmartTask.UnitTests.Services;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsProjectAndReturnsOwnerEmail()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser("owner@example.com", Role.ProjectManager);
        var service = new ProjectService(db.Projects, db.Users);

        var dto = await service.CreateAsync(owner.Id, new ProjectCreateDto { Name = "Alpha", Description = "First" });

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("Alpha", dto.Name);
        Assert.Equal("owner@example.com", dto.OwnerEmail);
        Assert.Single(db.Context.Projects);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        using var db = new TestDatabase();
        var service = new ProjectService(db.Projects, db.Users);

        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedOwnerEmail()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser("pm@example.com");
        var project = db.AddProject(owner.Id, "Alpha");
        var service = new ProjectService(db.Projects, db.Users);

        var dto = await service.GetByIdAsync(project.Id);

        Assert.NotNull(dto);
        Assert.Equal("pm@example.com", dto!.OwnerEmail);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        using var db = new TestDatabase();
        var service = new ProjectService(db.Projects, db.Users);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new ProjectCreateDto { Name = "Ghost" }));
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndDescription()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id, "Old");
        var service = new ProjectService(db.Projects, db.Users);

        await service.UpdateAsync(project.Id, new ProjectCreateDto { Name = "New", Description = "Updated" });

        var stored = await db.Context.Projects.FindAsync(project.Id);
        Assert.Equal("New", stored!.Name);
        Assert.Equal("Updated", stored.Description);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProject()
    {
        using var db = new TestDatabase();
        var owner = db.AddUser();
        var project = db.AddProject(owner.Id);
        var service = new ProjectService(db.Projects, db.Users);

        await service.DeleteAsync(project.Id);

        Assert.Empty(db.Context.Projects);
    }

    [Fact]
    public async Task GetPagedAsync_AsAdmin_ReturnsAllProjectsSortedByName()
    {
        using var db = new TestDatabase();
        var admin = db.AddUser("admin@example.com", Role.Admin);
        var other = db.AddUser("other@example.com", Role.ProjectManager);
        db.AddProject(admin.Id, "Zebra");
        db.AddProject(other.Id, "Alpha");
        var service = new ProjectService(db.Projects, db.Users);

        var result = await service.GetPagedAsync(new ProjectQueryDto(), admin.Id, "Admin");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "Alpha", "Zebra" }, result.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task GetPagedAsync_AsNonAdmin_OnlyReturnsOwnProjects()
    {
        using var db = new TestDatabase();
        var pm = db.AddUser("pm@example.com", Role.ProjectManager);
        var other = db.AddUser("other@example.com", Role.ProjectManager);
        db.AddProject(pm.Id, "Mine");
        db.AddProject(other.Id, "Theirs");
        var service = new ProjectService(db.Projects, db.Users);

        var result = await service.GetPagedAsync(new ProjectQueryDto(), pm.Id, "ProjectManager");

        Assert.Single(result.Items);
        Assert.Equal("Mine", result.Items[0].Name);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesKeywordPagingAndDescendingSort()
    {
        using var db = new TestDatabase();
        var admin = db.AddUser("admin@example.com", Role.Admin);
        db.AddProject(admin.Id, "Beta", "matches keyword", DateTime.UtcNow.AddDays(-1));
        db.AddProject(admin.Id, "Alpha", "matches keyword", DateTime.UtcNow.AddDays(-2));
        db.AddProject(admin.Id, "Gamma", "other text", DateTime.UtcNow.AddDays(-3));
        var service = new ProjectService(db.Projects, db.Users);

        var page = await service.GetPagedAsync(
            new ProjectQueryDto
            {
                Keyword = " matches ",
                SortBy = "CreatedAt",
                SortOrder = "desc",
                PageNumber = 1,
                PageSize = 1
            },
            admin.Id,
            "Admin");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
        Assert.Equal("Beta", page.Items[0].Name);
    }
}
