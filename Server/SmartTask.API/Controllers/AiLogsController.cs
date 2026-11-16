using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;

namespace SmartTask.API.Controllers;

/// <summary>
/// Exposes the AI provider call audit trail. Administrator access only.
/// </summary>
[ApiController]
[Route("api/ailogs")]
[Authorize(Roles = "Admin")]
public class AiLogsController : ControllerBase
{
    private readonly IAiInteractionLogService _aiInteractionLogService;
    private readonly IValidator<AiInteractionLogQueryDto> _queryValidator;

    public AiLogsController(IAiInteractionLogService aiInteractionLogService, IValidator<AiInteractionLogQueryDto> queryValidator)
    {
        _aiInteractionLogService = aiInteractionLogService;
        _queryValidator = queryValidator;
    }

    /// <summary>
    /// Returns successful AI provider calls, newest first, with optional keyword, model, user and date filters.
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<AiInteractionLogDto>>>> GetPagedAsync([FromQuery] AiInteractionLogQueryDto query, CancellationToken cancellationToken)
    {
        var validationResult = await _queryValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<PagedResult<AiInteractionLogDto>>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        var result = await _aiInteractionLogService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<AiInteractionLogDto>>.Ok(result));
    }
}
