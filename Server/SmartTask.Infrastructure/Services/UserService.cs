using Microsoft.EntityFrameworkCore;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Infrastructure.Auth;
using SmartTask.Infrastructure.Exceptions;
using SmartTask.Infrastructure.Persistence;
using SmartTask.Infrastructure.Repositories;

namespace SmartTask.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IRepository<User> userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserDto>> GetAssignableUsersAsync(Role? role, Guid currentUserId)
    {
        var query = _userRepository.AsQueryable()
            .Where(u => u.Role != Role.Admin && u.Id != currentUserId);

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        return await query
            .OrderBy(u => u.Name ?? u.Email)
            .Select(u => new UserDto { Id = u.Id, Email = u.Email, Name = u.Name, Role = u.Role })
            .ToListAsync();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        if (createUserDto.Role == Role.Admin)
        {
            throw new InvalidOperationException("Admin accounts cannot be created through the API.");
        }

        var email = Normalize(createUserDto.Email);
        if (await _userRepository.FindSingleAsync(u => u.Email == email) != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(createUserDto.Password),
            Name = createUserDto.Name,
            Role = createUserDto.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await UniqueConstraintGuard.SaveChangesOrThrowDuplicateEmailAsync(() => _userRepository.SaveChangesAsync());
        return Map(user);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileDto updateProfileDto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var email = Normalize(updateProfileDto.Email);
        var duplicate = await _userRepository.FindSingleAsync(u => u.Email == email && u.Id != userId);
        if (duplicate != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(updateProfileDto.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(updateProfileDto.CurrentPassword)
                || !_passwordHasher.Verify(updateProfileDto.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            if (!string.Equals(updateProfileDto.NewPassword, updateProfileDto.ConfirmPassword, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("New passwords do not match.");
            }

            user.PasswordHash = _passwordHasher.Hash(updateProfileDto.NewPassword);
        }
        user.Email = email;
        user.Name = updateProfileDto.Name;
        user.UpdatedAt = DateTime.UtcNow;
        await UniqueConstraintGuard.SaveChangesOrThrowDuplicateEmailAsync(() => _userRepository.SaveChangesAsync());
        return Map(user);
    }

    public async Task<UserDto> UpdateUserRoleAsync(Guid userId, Guid currentUserId, UpdateUserRoleDto updateUserRoleDto)
    {
        if (userId == currentUserId)
        {
            throw new InvalidOperationException("You cannot change your own role.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.Role == Role.Admin)
        {
            throw new InvalidOperationException("Admin accounts cannot be modified.");
        }

        if (updateUserRoleDto.Role != Role.ProjectManager)
        {
            throw new InvalidOperationException("Only promotion to ProjectManager is supported.");
        }

        if (user.Role != Role.TeamMember)
        {
            throw new InvalidOperationException("Only TeamMember accounts can be promoted to ProjectManager.");
        }

        user.Role = Role.ProjectManager;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();
        return Map(user);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static UserDto Map(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role
    };
}