using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;

namespace SmartTask.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAssignableUsersAsync(Role? role, Guid currentUserId);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileDto updateProfileDto);
}