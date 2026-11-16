using Microsoft.EntityFrameworkCore;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Entities;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Repositories;
using System.Linq;

namespace SmartTask.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<TaskItem> _taskItemRepository;

    public DashboardService(IRepository<Project> projectRepository, IRepository<TaskItem> taskItemRepository)
    {
        _projectRepository = projectRepository;
        _taskItemRepository = taskItemRepository;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid currentUserId, string currentRole)
    {
        var role = Enum.Parse<Role>(currentRole, ignoreCase: true);
        var now = DateTime.UtcNow;

        var projects = _projectRepository.AsQueryable();
        var tasks = _taskItemRepository.AsQueryable();

        string scope;
        switch (role)
        {
            case Role.Admin:
                scope = "All projects and tasks";
                break;
            case Role.ProjectManager:
                projects = projects.Where(p => p.ManagerId == currentUserId);
                tasks = tasks.Where(t => t.Project.ManagerId == currentUserId);
                scope = "Projects you manage";
                break;
            default:
                projects = projects.Where(p => p.Tasks.Any(t => t.AssigneeId == currentUserId));
                tasks = tasks.Where(t => t.AssigneeId == currentUserId);
                scope = "Tasks assigned to you";
                break;
        }

        var totalProjects = await projects.CountAsync(p => p.Status != ProjectStatus.Archived);
        var totalTasks = await tasks.CountAsync();
        var completedTasks = await tasks.CountAsync(t => t.Status == TaskStatus.Completed);
        var pendingTasks = await tasks.CountAsync(t => t.Status != TaskStatus.Completed);
        var upcomingDueTasks = await tasks.CountAsync(t => t.DueAt.HasValue && t.DueAt.Value > now && t.Status != TaskStatus.Completed);

        var statusGroups = await tasks.GroupBy(t => t.Status.ToString())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var priorityGroups = await tasks.GroupBy(t => t.Priority.ToString())
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardStatsDto
        {
            Scope = scope,
            TotalProjects = totalProjects,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            PendingTasks = pendingTasks,
            UpcomingDueTasks = upcomingDueTasks,
            TasksByStatus = statusGroups.ToDictionary(x => x.Status, x => x.Count),
            TasksByPriority = priorityGroups.ToDictionary(x => x.Priority, x => x.Count)
        };
    }
}
