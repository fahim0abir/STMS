using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;

namespace SmartTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IValidator<ProjectQueryDto> _projectQueryValidator;

    public ProjectsController(IProjectService projectService, IValidator<ProjectQueryDto> projectQueryValidator)
    {
        _projectService = projectService;
        _projectQueryValidator = projectQueryValidator;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateAsync([FromBody] ProjectCreateDto createDto)
    {
        var project = await _projectService.CreateAsync(GetUserId(), createDto);
        return Ok(ApiResponse<ProjectDto>.Ok(project, "Project created."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetByIdAsync(Guid id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound(ApiResponse<ProjectDto>.Error("Project not found.", 404));
        }
        return Ok(ApiResponse<ProjectDto>.Ok(project));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> GetPagedAsync([FromQuery] ProjectQueryDto query)
    {
        var validationResult = await _projectQueryValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<PagedResult<ProjectDto>>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }
        var result = await _projectService.GetPagedAsync(query, GetUserId(), GetUserRole());
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateAsync(Guid id, [FromBody] ProjectCreateDto createDto)
    {
        await _projectService.UpdateAsync(id, createDto);
        return Ok(ApiResponse<ProjectDto>.Ok(null, "Project updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(Guid id)
    {
        await _projectService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Project deleted."));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
