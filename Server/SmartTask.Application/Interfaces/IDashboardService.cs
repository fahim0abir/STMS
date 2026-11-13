using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}
