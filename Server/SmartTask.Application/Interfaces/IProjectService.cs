using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(Guid ownerId, ProjectCreateDto createDto);
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProjectDto>> GetByOwnerAsync(Guid ownerId);
    Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryDto query, Guid currentUserId, string currentUserRole);
    Task UpdateAsync(Guid id, ProjectCreateDto createDto);
    Task DeleteAsync(Guid id);
}
