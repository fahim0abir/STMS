using SmartTask.Application.DTOs;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Application.Interfaces;

public interface ITaskItemService
{
    Task<TaskItemDto> CreateAsync(Guid projectId, TaskItemCreateDto createDto);
    Task<TaskItemDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TaskItemDto>> GetByProjectAsync(Guid projectId);
    Task<PagedResult<TaskItemDto>> GetPagedAsync(TaskItemQueryDto query, Guid currentUserId, string currentUserRole);
    Task UpdateAsync(Guid id, TaskItemCreateDto createDto);
    Task UpdateStatusAsync(Guid id, TaskStatus status);
    Task DeleteAsync(Guid id);
}
