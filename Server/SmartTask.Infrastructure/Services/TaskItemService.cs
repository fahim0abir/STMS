using Microsoft.EntityFrameworkCore;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Repositories;
using System.Linq;

namespace SmartTask.Infrastructure.Services;

public class TaskItemService : ITaskItemService
{
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<User> _userRepository;

    public TaskItemService(IRepository<TaskItem> taskRepository, IRepository<Project> projectRepository, IRepository<User> userRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<TaskItemDto> CreateAsync(Guid projectId, TaskItemCreateDto createDto, Guid currentUserId, string currentRole)
    {
        var role = Enum.Parse<Role>(currentRole, ignoreCase: true);
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
            throw new NotFoundException(nameof(Project), projectId);

        EnsureCanManageProject(project, currentUserId, role);
        await EnsureAssigneeIsTeamMemberAsync(createDto.AssigneeId, currentUserId);

        var taskItem = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = createDto.Title,
            Description = createDto.Description,
            Status = createDto.Status,
            Priority = createDto.Priority,
            AssigneeId = createDto.AssigneeId,
            DueAt = createDto.DueAt,
            AiSuggestionJson = createDto.AiSuggestionJson,
            CreatedAt = DateTime.UtcNow
        };
        await _taskRepository.AddAsync(taskItem);
        await _taskRepository.SaveChangesAsync();
        var assignee = taskItem.AssigneeId.HasValue ? await _userRepository.GetByIdAsync(taskItem.AssigneeId.Value) : null;
        return Map(taskItem, assignee, project.Name);
    }

    public async Task<TaskItemDto?> GetByIdAsync(Guid id)
    {
        var taskItem = await _taskRepository.AsQueryable()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (taskItem == null) return null;
        var assignee = taskItem.AssigneeId.HasValue ? await _userRepository.GetByIdAsync(taskItem.AssigneeId.Value) : null;
        return Map(taskItem, assignee, taskItem.Project.Name);
    }

    public async Task<IEnumerable<TaskItemDto>> GetByProjectAsync(Guid projectId)
    {
        var items = await _taskRepository.AsQueryable()
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskItemDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                AssigneeId = t.AssigneeId,
                AssigneeEmail = t.Assignee != null ? t.Assignee.Email : null,
                AssigneeName = t.Assignee != null ? t.Assignee.Name : null,
                DueAt = t.DueAt,
                AiSuggestionJson = t.AiSuggestionJson,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
        return items;
    }

    public async Task<PagedResult<TaskItemDto>> GetPagedAsync(TaskItemQueryDto query, Guid currentUserId, string currentUserRole)
    {
        var role = Enum.Parse<Role>(currentUserRole, ignoreCase: true);
        IQueryable<TaskItem> q = _taskRepository.AsQueryable();

        q = ApplyRoleFilter(q, role, currentUserId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(t => t.Title.Contains(kw) || (t.Description != null && t.Description.Contains(kw)));
        }

        if (query.ProjectId.HasValue) q = q.Where(t => t.ProjectId == query.ProjectId.Value);
        if (query.PendingOnly) q = q.Where(t => t.Status != SmartTask.Domain.Enums.TaskStatus.Completed);
        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);
        if (query.Priority.HasValue) q = q.Where(t => t.Priority == query.Priority.Value);
        if (query.AssigneeId.HasValue) q = q.Where(t => t.AssigneeId == query.AssigneeId.Value);
        if (query.DueFrom.HasValue) q = q.Where(t => t.DueAt.HasValue && t.DueAt.Value >= query.DueFrom.Value);
        if (query.DueTo.HasValue) q = q.Where(t => t.DueAt.HasValue && t.DueAt.Value <= query.DueTo.Value);

        q = ApplySorting(q, query.SortBy, query.SortOrder);

        var dtoQuery = q.Select(t => new TaskItemDto
        {
            Id = t.Id,
            ProjectId = t.ProjectId,
            ProjectName = t.Project.Name,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
            AssigneeEmail = t.Assignee != null ? t.Assignee.Email : null,
            AssigneeName = t.Assignee != null ? t.Assignee.Name : null,
            AssigneeId = t.AssigneeId,
            DueAt = t.DueAt,
            AiSuggestionJson = t.AiSuggestionJson,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });

        return await dtoQuery.ToPagedResultAsync(query.PageNumber, query.PageSize);
    }

    public async Task UpdateAsync(Guid id, TaskItemCreateDto createDto, Guid currentUserId, string currentRole)
    {
        var role = Enum.Parse<Role>(currentRole, ignoreCase: true);
        var taskItem = await _taskRepository.AsQueryable()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (taskItem == null)
            throw new NotFoundException(nameof(TaskItem), id);

        EnsureCanManageProject(taskItem.Project, currentUserId, role);
        await EnsureAssigneeIsTeamMemberAsync(createDto.AssigneeId, currentUserId, taskItem.AssigneeId);

        taskItem.Title = createDto.Title;
        taskItem.Description = createDto.Description;
        taskItem.Status = createDto.Status;
        taskItem.Priority = createDto.Priority;
        taskItem.AssigneeId = createDto.AssigneeId;
        taskItem.DueAt = createDto.DueAt;
        taskItem.AiSuggestionJson = createDto.AiSuggestionJson;
        taskItem.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid id, SmartTask.Domain.Enums.TaskStatus status)
    {
        var taskItem = await _taskRepository.GetByIdAsync(id);
        if (taskItem == null)
            throw new NotFoundException(nameof(TaskItem), id);

        taskItem.Status = status;
        taskItem.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var taskItem = await _taskRepository.GetByIdAsync(id);
        if (taskItem == null)
            throw new NotFoundException(nameof(TaskItem), id);
        await _taskRepository.RemoveAsync(taskItem);
        await _taskRepository.SaveChangesAsync();
    }

    public async Task AssignAsync(Guid id, Guid? assigneeId, Guid currentUserId, string currentRole)
    {
        var role = Enum.Parse<Role>(currentRole, ignoreCase: true);
        var taskItem = await _taskRepository.AsQueryable()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (taskItem == null)
            throw new NotFoundException(nameof(TaskItem), id);

        EnsureCanManageProject(taskItem.Project, currentUserId, role);
        await EnsureAssigneeIsTeamMemberAsync(assigneeId, currentUserId, taskItem.AssigneeId);

        taskItem.AssigneeId = assigneeId;
        taskItem.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.SaveChangesAsync();
    }

    private static void EnsureCanManageProject(Project project, Guid currentUserId, Role role)
    {
        if (role == Role.Admin) return;
        if (role == Role.ProjectManager && project.ManagerId == currentUserId) return;

        throw new ForbiddenException("Only the assigned project manager or an administrator can manage tasks in this project.");
    }

    private async Task EnsureAssigneeIsTeamMemberAsync(Guid? assigneeId, Guid currentUserId, Guid? currentAssigneeId = null)
    {
        if (!assigneeId.HasValue) return;

        var assignee = await _userRepository.GetByIdAsync(assigneeId.Value);
        if (assignee == null)
            throw new NotFoundException(nameof(User), assigneeId.Value);

        if (assignee.Id == currentUserId && assignee.Id != currentAssigneeId)
            throw new InvalidOperationException("You cannot assign a task to yourself.");

        if (assignee.Role != Role.TeamMember)
            throw new InvalidOperationException("Tasks can only be assigned to team members.");
    }

    private static TaskItemDto Map(TaskItem t, User? assignee, string projectName) => new()
    {
        Id = t.Id,
        ProjectId = t.ProjectId,
        ProjectName = projectName,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        AssigneeId = t.AssigneeId,
        AssigneeEmail = assignee?.Email,
        AssigneeName = assignee?.Name,
        DueAt = t.DueAt,
        AiSuggestionJson = t.AiSuggestionJson,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    private static IQueryable<TaskItem> ApplyRoleFilter(IQueryable<TaskItem> q, Role role, Guid currentUserId)
    {
        return role switch
        {
            Role.Admin => q,
            Role.ProjectManager => q.Where(t => t.Project.ManagerId == currentUserId),
            _ => q.Where(t => t.AssigneeId == currentUserId)
        };
    }

    private static IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> q, string? sortBy, string? sortOrder)
    {
        var field = ResolveSortField(sortBy, TaskItemQueryDto.AllowedSortFields, TaskItemQueryDto.DefaultSortBy);
        var desc = IsDescending(sortOrder);
        return field switch
        {
            "Title" => desc ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
            "Status" => desc ? q.OrderByDescending(t => t.Status) : q.OrderBy(t => t.Status),
            "Priority" => desc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "DueAt" => desc ? q.OrderByDescending(t => t.DueAt) : q.OrderBy(t => t.DueAt),
            "CreatedAt" => desc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
            _ => q.OrderByDescending(t => t.CreatedAt)
        };
    }

    private static string ResolveSortField(string? sortBy, string[] allowed, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(sortBy) && allowed.Contains(sortBy.Trim()))
            return sortBy.Trim();
        return fallback;
    }

    private static bool IsDescending(string? sortOrder) =>
        string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
}