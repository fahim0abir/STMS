using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces;

public interface ITaskDescriptionEnhancementService
{
    Task<string> EnhanceAsync(string description);
}
