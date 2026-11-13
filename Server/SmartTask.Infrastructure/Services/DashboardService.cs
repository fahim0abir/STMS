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

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var tasks = _taskItemRepository.AsQueryable();

        var totalProjects = await _projectRepository.AsQueryable().CountAsync();
        var totalTasks = await tasks.CountAsync();
        var completedTasks = await tasks.CountAsync(t => t.Status == TaskStatus.Done);
        var pendingTasks = await tasks.CountAsync(t => t.Status != TaskStatus.Done);
        var upcomingDueTasks = await tasks.CountAsync(t => t.DueAt.HasValue && t.DueAt.Value > now && t.Status != TaskStatus.Done);

        var statusGroups = await tasks.GroupBy(t => t.Status.ToString())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var priorityGroups = await tasks.GroupBy(t => t.Priority.ToString())
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardStatsDto
        {
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
