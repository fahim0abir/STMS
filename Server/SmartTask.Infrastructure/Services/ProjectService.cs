using Microsoft.EntityFrameworkCore;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Repositories;
using System.Linq;

namespace SmartTask.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<User> _userRepository;

    public ProjectService(IRepository<Project> projectRepository, IRepository<User> userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<ProjectDto> CreateAsync(Guid currentUserId, string currentRole, ProjectCreateDto createDto)
    {
        var role = Enum.Parse<Role>(currentRole, ignoreCase: true);
        var managerId = currentUserId;

        if (createDto.ManagerId.HasValue && createDto.ManagerId.Value != Guid.Empty)
        {
            var selectedManager = await _userRepository.GetByIdAsync(createDto.ManagerId.Value);
            if (selectedManager == null)
                throw new NotFoundException(nameof(User), createDto.ManagerId.Value);

            if (selectedManager.Role != Role.ProjectManager)
                throw new InvalidOperationException("The selected user must have the ProjectManager role.");

            if (role != Role.Admin && selectedManager.Id != currentUserId)
                throw new ForbiddenException("Only an administrator can assign a project to another manager.");

            managerId = selectedManager.Id;
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Description = createDto.Description,
            ManagerId = managerId,
            CreatedAt = DateTime.UtcNow
        };
        await _projectRepository.AddAsync(project);
        await _projectRepository.SaveChangesAsync();
        var manager = await _userRepository.GetByIdAsync(managerId);
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            ManagerId = project.ManagerId,
            ManagerEmail = manager?.Email ?? string.Empty,
            ManagerName = manager?.Name
        };
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        var project = await _projectRepository.AsQueryable()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                ManagerId = p.ManagerId,
                ManagerEmail = p.Manager.Email,
                ManagerName = p.Manager.Name
            })
            .FirstOrDefaultAsync();
        return project;
    }

    public async Task<IEnumerable<ProjectDto>> GetByManagerAsync(Guid managerId)
    {
        var projects = await _projectRepository.AsQueryable()
            .Where(p => p.ManagerId == managerId)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                ManagerId = p.ManagerId,
                ManagerEmail = p.Manager.Email,
                ManagerName = p.Manager.Name
            })
            .ToListAsync();
        return projects;
    }

    public async Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryDto query, Guid currentUserId, string currentUserRole)
    {
        var role = Enum.Parse<Role>(currentUserRole, ignoreCase: true);
        IQueryable<Project> q = _projectRepository.AsQueryable();

        q = ApplyRoleFilter(q, role, currentUserId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(p => p.Name.Contains(kw) || (p.Description != null && p.Description.Contains(kw)));
        }

        q = ApplySorting(q, query.SortBy, query.SortOrder);

        var dtoQuery = q.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            ManagerId = p.ManagerId,
            ManagerEmail = p.Manager.Email,
            ManagerName = p.Manager.Name
        });

        return await dtoQuery.ToPagedResultAsync(query.PageNumber, query.PageSize);
    }

    public async Task AssignManagerAsync(Guid id, Guid managerId)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            throw new NotFoundException(nameof(Project), id);

        var manager = await _userRepository.GetByIdAsync(managerId);
        if (manager == null)
            throw new NotFoundException(nameof(User), managerId);

        if (manager.Role != Role.ProjectManager)
            throw new InvalidOperationException("The selected user must have the ProjectManager role.");

        project.ManagerId = managerId;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guid id, ProjectCreateDto createDto)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            throw new NotFoundException(nameof(Project), id);
        project.Name = createDto.Name;
        project.Description = createDto.Description;
        project.UpdatedAt = DateTime.UtcNow;
        await _projectRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            throw new NotFoundException(nameof(Project), id);
        await _projectRepository.RemoveAsync(project);
        await _projectRepository.SaveChangesAsync();
    }

    private static ProjectDto Map(Project p, string managerEmail) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        ManagerId = p.ManagerId,
        ManagerEmail = managerEmail
    };

    private static IQueryable<Project> ApplyRoleFilter(IQueryable<Project> q, Role role, Guid currentUserId)
    {
        return role switch
        {
            Role.Admin => q,
            _ => q.Where(p => p.ManagerId == currentUserId)
        };
    }

    private static IQueryable<Project> ApplySorting(IQueryable<Project> q, string? sortBy, string? sortOrder)
    {
        var field = ResolveSortField(sortBy, ProjectQueryDto.AllowedSortFields, ProjectQueryDto.DefaultSortBy);
        var desc = IsDescending(sortOrder);
        return field switch
        {
            "Name" => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
            "CreatedAt" => desc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            _ => q.OrderBy(p => p.Name)
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