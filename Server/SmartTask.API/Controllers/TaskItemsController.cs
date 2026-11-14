using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Enums;

namespace SmartTask.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskItemsController : ControllerBase
{
    private readonly ITaskItemService _taskItemService;
    private readonly IValidator<TaskItemQueryDto> _taskItemQueryValidator;
    private readonly IValidator<TaskAssigneeAssignDto> _taskAssigneeAssignValidator;

    public TaskItemsController(
        ITaskItemService taskItemService,
        IValidator<TaskItemQueryDto> taskItemQueryValidator,
        IValidator<TaskAssigneeAssignDto> taskAssigneeAssignValidator)
    {
        _taskItemService = taskItemService;
        _taskItemQueryValidator = taskItemQueryValidator;
        _taskAssigneeAssignValidator = taskAssigneeAssignValidator;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> CreateAsync([FromQuery] Guid projectId, [FromBody] TaskItemCreateDto createDto)
    {
        var taskItem = await _taskItemService.CreateAsync(projectId, createDto, GetUserId(), GetUserRole());
        return Ok(ApiResponse<TaskItemDto>.Ok(taskItem, "Task item created."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> GetByIdAsync(Guid id)
    {
        var taskItem = await _taskItemService.GetByIdAsync(id);
        if (taskItem == null)
        {
            return NotFound(ApiResponse<TaskItemDto>.Error("Task item not found.", 404));
        }
        return Ok(ApiResponse<TaskItemDto>.Ok(taskItem));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<TaskItemDto>>>> GetPagedAsync([FromQuery] TaskItemQueryDto query)
    {
        var validationResult = await _taskItemQueryValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<PagedResult<TaskItemDto>>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }
        var result = await _taskItemService.GetPagedAsync(query, GetUserId(), GetUserRole());
        return Ok(ApiResponse<PagedResult<TaskItemDto>>.Ok(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskItemDto>>>> GetByProjectAsync([FromQuery] Guid projectId)
    {
        var taskItems = await _taskItemService.GetByProjectAsync(projectId);
        return Ok(ApiResponse<IEnumerable<TaskItemDto>>.Ok(taskItems));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> UpdateAsync(Guid id, [FromBody] TaskItemCreateDto createDto)
    {
        await _taskItemService.UpdateAsync(id, createDto, GetUserId(), GetUserRole());
        return Ok(ApiResponse<TaskItemDto>.Ok(null, "Task item updated."));
    }

    [HttpPatch("{id:guid}/assignee")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<ActionResult<ApiResponse<object>>> AssignAsync(Guid id, [FromBody] TaskAssigneeAssignDto assignDto)
    {
        var validationResult = await _taskAssigneeAssignValidator.ValidateAsync(assignDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<object>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        await _taskItemService.AssignAsync(id, assignDto.AssigneeId, GetUserId(), GetUserRole());
        return Ok(ApiResponse<object>.Ok(null, "Task assignment updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(Guid id)
    {
        await _taskItemService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Task item deleted."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatusAsync(Guid id, [FromBody] TaskStatusUpdateDto statusDto)
    {
        if (!Enum.IsDefined(statusDto.Status))
        {
            return BadRequest(ApiResponse<object>.Error("Unknown task status.", 400));
        }

        var taskItem = await _taskItemService.GetByIdAsync(id);
        if (taskItem == null)
        {
            return NotFound(ApiResponse<object>.Error("Task item not found.", 404));
        }

        var role = GetUserRole();
        var isManager = role is "Admin" or "ProjectManager";
        var isAssignee = taskItem.AssigneeId.HasValue && taskItem.AssigneeId.Value == GetUserId();
        if (!isManager && !isAssignee)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Error("Only the assignee or a manager can change this task status.", 403));
        }

        await _taskItemService.UpdateStatusAsync(id, statusDto.Status);
        return Ok(ApiResponse<object>.Ok(null, "Task status updated."));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
