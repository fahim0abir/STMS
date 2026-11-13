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
    /// Enhances a task description using AI for grammar, clarity, and professional tone.
    /// </summary>
    /// <param name="request">The description to enhance (5-1,000 characters).</param>
    /// <returns>The enhanced description.</returns>
    [HttpPost("enhance-description")]
    [EnableRateLimiting("enhance")]
    public async Task<ActionResult<ApiResponse<string>>> EnhanceDescriptionAsync([FromBody] EnhanceDescriptionRequestDto request)
    {
        var validationResult = await _enhanceValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<string>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        var enhanced = await _enhancementService.EnhanceAsync(request.Description);
        return Ok(ApiResponse<string>.Ok(enhanced, "Description enhanced."));
    }
}
