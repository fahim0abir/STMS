using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Persistence;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.E2ETests.Infrastructure;

public static class TestData
{
    public const string AdminEmail = "admin@smarttask.dev";
    public const string AdminPassword = "Admin123!";
    public const string ManagerEmail = "projectmanager@smarttask.dev";
    public const string ManagerPassword = "Pm123456!";
    public const string MemberEmail = "teammember@smarttask.dev";
    public const string MemberPassword = "Tm123456!";

    public static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid ManagerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid MemberId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public static readonly Guid AdminProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ManagerProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid MemberAssignedTaskId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid AdminAssignedTaskId = Guid.Parse("33333333-3333-3333-3333-333333333332");
    public static readonly Guid CompletedTaskId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ManagerProjectTaskId = Guid.Parse("33333333-3333-3333-3333-333333333334");
}

public static class TestDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await context.Projects.AnyAsync())
        {
            return;
        }

        context.Projects.AddRange(
            new Project
            {
                Id = TestData.AdminProjectId,
                Name = "Alpha Website Redesign",
                Description = "Marketing site refresh owned by the administrator",
                OwnerId = TestData.AdminId,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Project
            {
                Id = TestData.ManagerProjectId,
                Name = "Beta Mobile App",
                Description = "Field operations app owned by the project manager",
                OwnerId = TestData.ManagerId,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });

        context.TaskItems.AddRange(
            new TaskItem
            {
                Id = TestData.MemberAssignedTaskId,
                ProjectId = TestData.AdminProjectId,
                Title = "Draft landing page copy",
                Description = "Write the hero and pricing copy",
                Status = TaskStatus.ToDo,
                Priority = TaskPriority.High,
                AssigneeId = TestData.MemberId,
                DueAt = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            },
            new TaskItem
            {
                Id = TestData.AdminAssignedTaskId,
                ProjectId = TestData.AdminProjectId,
                Title = "Review accessibility audit",
                Description = "Triage the WCAG findings",
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                AssigneeId = TestData.AdminId,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new TaskItem
            {
                Id = TestData.CompletedTaskId,
                ProjectId = TestData.AdminProjectId,
                Title = "Set up CI pipeline",
                Description = "Build and test on every push",
                Status = TaskStatus.Completed,
                Priority = TaskPriority.Low,
                AssigneeId = TestData.MemberId,
                CreatedAt = DateTime.UtcNow.AddDays(-9)
            },
            new TaskItem
            {
                Id = TestData.ManagerProjectTaskId,
                ProjectId = TestData.ManagerProjectId,
                Title = "Prototype offline sync",
                Description = "Queue writes while the device is offline",
                Status = TaskStatus.ToDo,
                Priority = TaskPriority.Critical,
                AssigneeId = TestData.MemberId,
                DueAt = DateTime.UtcNow.AddDays(5),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });

        await context.SaveChangesAsync();
    }
}
