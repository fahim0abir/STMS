using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces;

public interface ITaskDescriptionEnhancementService
{
    Task<EnhanceDescriptionResponseDto> EnhanceAsync(EnhanceDescriptionRequestDto request, AiRequestContext context, CancellationToken cancellationToken = default);
}
