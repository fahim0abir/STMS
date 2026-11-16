using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Entities;

namespace SmartTask.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Name).HasMaxLength(100);
            entity.HasMany(u => u.RefreshTokens).WithOne(r => r.User).HasForeignKey(r => r.UserId);
            entity.HasMany(u => u.ManagedProjects).WithOne(p => p.Manager).HasForeignKey(p => p.ManagerId);
            entity.HasMany(u => u.Tasks).WithOne(t => t.Assignee).HasForeignKey(t => t.AssigneeId).IsRequired(false);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).IsRequired().HasMaxLength(256);
            entity.Property(r => r.JwtId).HasMaxLength(256);
            entity.HasOne(r => r.User).WithMany(u => u.RefreshTokens).HasForeignKey(r => r.UserId);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.ClientName).HasMaxLength(200);
            entity.Property(p => p.TechStack).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Status).IsRequired();
            entity.Property(p => p.StartDate).IsRequired();
            entity.HasOne(p => p.Manager).WithMany(u => u.ManagedProjects).HasForeignKey(p => p.ManagerId);
            entity.HasMany(p => p.Tasks).WithOne(t => t.Project).HasForeignKey(t => t.ProjectId);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(4000);
            entity.Property(t => t.AiSuggestionJson);
            entity.HasOne(t => t.Project).WithMany(p => p.Tasks).HasForeignKey(t => t.ProjectId);
            entity.HasOne(t => t.Assignee).WithMany(u => u.Tasks).HasForeignKey(t => t.AssigneeId).IsRequired(false);
        });
    }
}
