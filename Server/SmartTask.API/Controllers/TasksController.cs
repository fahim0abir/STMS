using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;

namespace SmartTask.API.Controllers;

/// <summary>
/// Provides task-related operations such as AI description enhancement.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskDescriptionEnhancementService _enhancementService;
    private readonly IValidator<EnhanceDescriptionRequestDto> _enhanceValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TasksController"/> class.
    /// </summary>
    /// <param name="enhancementService">The AI description enhancement service.</param>
    /// <param name="enhanceValidator">The validator for enhance-description requests.</param>
    public TasksController(ITaskDescriptionEnhancementService enhancementService, IValidator<EnhanceDescriptionRequestDto> enhanceValidator)
    {
        _enhancementService = enhancementService;
        _enhanceValidator = enhanceValidator;
    }

    /// <summary>
    /// Enhances a task description using AI for grammar, clarity, and a structured, actionable definition.
    /// </summary>
    /// <param name="request">The description to enhance (20-4,000 characters) with optional title and context.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Structured enhancement result (description, title, acceptance criteria, subtasks, priority, estimate).</returns>
    [HttpPost("enhance-description")]
    [EnableRateLimiting("enhance")]
    public async Task<ActionResult<ApiResponse<EnhanceDescriptionResponseDto>>> EnhanceDescriptionAsync([FromBody] EnhanceDescriptionRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _enhanceValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<EnhanceDescriptionResponseDto>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        try
        {
            var result = await _enhancementService.EnhanceAsync(request, BuildAiContext(), cancellationToken);
            var message = result.UsedFallback
                ? "Description enhanced with the local fallback (AI provider unavailable)."
                : "Description enhanced.";
            return Ok(ApiResponse<EnhanceDescriptionResponseDto>.Ok(result, message));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, ApiResponse<EnhanceDescriptionResponseDto>.Error("The AI provider did not respond in time. Please retry.", 504));
        }
    }

    private AiRequestContext BuildAiContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return new AiRequestContext
        {
            UserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null,
            UserEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty,
            UserName = User.FindFirstValue(ClaimTypes.Name),
            UserRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
        };
    }
}
