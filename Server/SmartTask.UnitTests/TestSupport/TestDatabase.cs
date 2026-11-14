using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Persistence;
using SmartTask.Infrastructure.Repositories;
using TaskPriority = SmartTask.Domain.Enums.TaskPriority;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.UnitTests.TestSupport;

/// <summary>
/// Builds an isolated in-memory <see cref="ApplicationDbContext"/> plus real repositories
/// so services are exercised against the EF Core query pipeline instead of bespoke fakes.
/// </summary>
internal sealed class TestDatabase : IDisposable
{
    public TestDatabase()
    {
        Context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"stms-tests-{Guid.NewGuid():N}")
                .Options);
    }

    public ApplicationDbContext Context { get; }

    public IRepository<User> Users => new Repository<User>(Context);

    public IRepository<Project> Projects => new Repository<Project>(Context);

    public IRepository<TaskItem> Tasks => new Repository<TaskItem>(Context);

    public IRefreshTokenRepository RefreshTokens => new RefreshTokenRepository(Context);

    public User AddUser(string email = "owner@example.com", Role role = Role.ProjectManager, string? name = "Owner")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            Name = name,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    public Project AddProject(Guid managerId, string name = "Project", string? description = null, DateTime? createdAt = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ManagerId = managerId,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        Context.Projects.Add(project);
        Context.SaveChanges();
        return project;
    }

    public TaskItem AddTask(
        Guid projectId,
        string title = "Task",
        TaskStatus status = TaskStatus.ToDo,
        TaskPriority priority = TaskPriority.Medium,
        Guid? assigneeId = null,
        DateTime? dueAt = null,
        DateTime? createdAt = null)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title,
            Status = status,
            Priority = priority,
            AssigneeId = assigneeId,
            DueAt = dueAt,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        Context.TaskItems.Add(task);
        Context.SaveChanges();
        return task;
    }

    public RefreshToken AddRefreshToken(Guid userId, string token, DateTime expiresAt, bool isUsed = false)
    {
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            JwtId = Guid.NewGuid().ToString(),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsUsed = isUsed,
            IsRevoked = false
        };
        Context.RefreshTokens.Add(entity);
        Context.SaveChanges();
        return entity;
    }

    public void Dispose() => Context.Dispose();
}
