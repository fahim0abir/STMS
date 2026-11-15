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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserDto> _createUserValidator;
    private readonly IValidator<UpdateProfileDto> _updateProfileValidator;
    private readonly IValidator<UpdateUserRoleDto> _updateUserRoleValidator;

    public UsersController(
        IUserService userService,
        IValidator<CreateUserDto> createUserValidator,
        IValidator<UpdateProfileDto> updateProfileValidator,
        IValidator<UpdateUserRoleDto> updateUserRoleValidator)
    {
        _userService = userService;
        _createUserValidator = createUserValidator;
        _updateProfileValidator = updateProfileValidator;
        _updateUserRoleValidator = updateUserRoleValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAsync([FromQuery] Role? role)
    {
        var users = await _userService.GetAssignableUsersAsync(role, GetUserId());
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateAsync([FromBody] CreateUserDto createUserDto)
    {
        var validationResult = await _createUserValidator.ValidateAsync(createUserDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        var user = await _userService.CreateUserAsync(createUserDto);
        return Ok(ApiResponse<UserDto>.Ok(user, "User created."));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfileAsync([FromBody] UpdateProfileDto updateProfileDto)
    {
        var validationResult = await _updateProfileValidator.ValidateAsync(updateProfileDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        var user = await _userService.UpdateProfileAsync(GetUserId(), updateProfileDto);
        return Ok(ApiResponse<UserDto>.Ok(user, "Profile updated."));
    }

    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateRoleAsync(Guid id, [FromBody] UpdateUserRoleDto updateUserRoleDto)
    {
        var validationResult = await _updateUserRoleValidator.ValidateAsync(updateUserRoleDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Error(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), 400));
        }

        var user = await _userService.UpdateUserRoleAsync(id, GetUserId(), updateUserRoleDto);
        return Ok(ApiResponse<UserDto>.Ok(user, "Role updated."));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}