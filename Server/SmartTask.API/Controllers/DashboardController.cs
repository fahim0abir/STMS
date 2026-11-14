using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;

namespace SmartTask.API.Controllers;

/// <summary>
/// Provides dashboard statistics for authenticated users.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="dashboardService">The dashboard statistics service.</param>
    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Retrieves aggregated dashboard statistics.
    /// </summary>
    /// <returns>Dashboard stats including project/task counts and breakdowns.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStatsAsync()
    {
        var stats = await _dashboardService.GetStatsAsync(GetUserId(), GetUserRole());
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard stats retrieved."));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
