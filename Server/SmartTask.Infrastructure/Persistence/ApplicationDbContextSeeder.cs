using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Enums;
using SmartTask.Domain.Entities;
using BCrypt.Net;

namespace SmartTask.Infrastructure.Persistence;

public class ApplicationDbContextSeeder
{
    private static readonly (Guid Id, string Email, string Password, string Name, Role Role)[] SeedUsers =
    [
        (Guid.Parse("00000000-0000-0000-0000-000000000001"), "admin@smarttask.dev", "Admin123!", "System Administrator", Role.Admin),
        (Guid.Parse("00000000-0000-0000-0000-000000000002"), "projectmanager@smarttask.dev", "Pm123456!", "Project Manager", Role.ProjectManager),
        (Guid.Parse("00000000-0000-0000-0000-000000000003"), "teammember@smarttask.dev", "Tm123456!", "Team Member", Role.TeamMember)
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var seedIds = SeedUsers.Select(u => u.Id).ToArray();
        var existingIds = await context.Users
            .Where(u => seedIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        var missingUsers = SeedUsers.Where(u => !existingIds.Contains(u.Id)).ToList();

        if (missingUsers.Count == 0)
        {
            return;
        }

        foreach (var seed in missingUsers)
        {
            context.Users.Add(new User
            {
                Id = seed.Id,
                Email = seed.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seed.Password),
                Name = seed.Name,
                Role = seed.Role,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}
