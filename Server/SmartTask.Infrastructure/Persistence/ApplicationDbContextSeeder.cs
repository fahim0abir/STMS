using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Enums;
using SmartTask.Domain.Entities;
using BCrypt.Net;

namespace SmartTask.Infrastructure.Persistence;

public class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Email = "admin@smarttask.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Name = "System Administrator",
            Role = Role.Admin,
            CreatedAt = DateTime.UtcNow
        };
        var projectManager = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Email = "projectmanager@smarttask.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pm123456!"),
            Name = "Project Manager",
            Role = Role.ProjectManager,
            CreatedAt = DateTime.UtcNow
        };
        var teamMember = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Email = "teammember@smarttask.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tm123456!"),
            Name = "Team Member",
            Role = Role.TeamMember,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(admin, projectManager, teamMember);
        await context.SaveChangesAsync();
    }
}
