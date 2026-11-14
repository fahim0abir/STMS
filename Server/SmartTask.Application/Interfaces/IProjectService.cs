using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(Guid currentUserId, string currentRole, ProjectCreateDto createDto);
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProjectDto>> GetByManagerAsync(Guid managerId);
    Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryDto query, Guid currentUserId, string currentUserRole);
    Task UpdateAsync(Guid id, ProjectCreateDto createDto);
    Task AssignManagerAsync(Guid id, Guid managerId);
    Task DeleteAsync(Guid id);
}
