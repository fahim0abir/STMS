using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces;

public interface IAiInteractionLogService
{
    Task LogSuccessAsync(AiInteractionLogCreateDto log, CancellationToken cancellationToken = default);
    Task<PagedResult<AiInteractionLogDto>> GetPagedAsync(AiInteractionLogQueryDto query, CancellationToken cancellationToken = default);
}
