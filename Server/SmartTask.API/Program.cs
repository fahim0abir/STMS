using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartTask.Application.Interfaces;
using SmartTask.Application.Validators;
using SmartTask.API.Middlewares;
using SmartTask.Domain;
using SmartTask.Infrastructure.Auth;
using SmartTask.Infrastructure.Persistence;
using SmartTask.Infrastructure.Repositories;
using SmartTask.Infrastructure.Services;
using FluentValidation;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using Serilog;
using SmartTask.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "app-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

if (builder.Environment.IsEnvironment("E2E"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("SmartTaskE2E"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));
}

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

builder.Services.AddScoped<IRepository<User>, Repository<User>>();
builder.Services.AddScoped<IRepository<Project>, Repository<Project>>();
builder.Services.AddScoped<IRepository<TaskItem>, Repository<TaskItem>>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITaskDescriptionEnhancementService, TaskDescriptionEnhancementService>();
builder.Services.AddScoped<IValidator<ProjectQueryDto>, ProjectQueryDtoValidator>();
builder.Services.AddScoped<IValidator<TaskItemQueryDto>, TaskItemQueryDtoValidator>();
builder.Services.AddScoped<IValidator<EnhanceDescriptionRequestDto>, EnhanceDescriptionRequestDtoValidator>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateProfileDto>, UpdateProfileDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRoleDto>, UpdateUserRoleDtoValidator>();
builder.Services.AddScoped<IValidator<ProjectManagerAssignDto>, ProjectManagerAssignDtoValidator>();
builder.Services.AddScoped<IValidator<TaskAssigneeAssignDto>, TaskAssigneeAssignDtoValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key 'Jwt:Key' is not configured.")))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Role.Admin.ToString()));
    options.AddPolicy("ProjectManagerOnly", policy => policy.RequireRole(Role.ProjectManager.ToString()));
    options.AddPolicy("TeamMemberOnly", policy => policy.RequireRole(Role.TeamMember.ToString()));
    options.AddPolicy("AdminOrProjectManager", policy => policy.RequireRole(Role.Admin.ToString(), Role.ProjectManager.ToString()));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddRateLimiter(options =>
{
    var configuredLimits = builder.Configuration.GetSection("RateLimiting");
    var permit = (string key, int fallback) => configuredLimits.GetValue<int?>(key) ?? fallback;

    options.AddFixedWindowLimiter("default", opts =>
    {
        opts.Window = TimeSpan.FromMinutes(1);
        opts.PermitLimit = permit("Default", 100);
    });
    options.AddFixedWindowLimiter("auth", opts =>
    {
        opts.Window = TimeSpan.FromMinutes(1);
        opts.PermitLimit = permit("Auth", 5);
    });
    options.AddFixedWindowLimiter("enhance", opts =>
    {
        opts.Window = TimeSpan.FromMinutes(1);
        opts.PermitLimit = permit("Enhance", 10);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

static async Task ApplyMigrationsAsync(ApplicationDbContext context)
{
    const int maxAttempts = 5;
    var retryDelay = TimeSpan.FromSeconds(5);

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            Log.Information("Applying database migrations (attempt {Attempt}/{MaxAttempts})...", attempt, maxAttempts);
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Log.Warning(ex, "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds}s...", attempt, maxAttempts, retryDelay.TotalSeconds);
            await Task.Delay(retryDelay);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration failed after {MaxAttempts} attempts. Aborting startup.", maxAttempts);
            throw;
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (context.Database.IsRelational() && app.Configuration.GetValue("Database:RunMigrationsOnStartup", true))
    {
        await ApplyMigrationsAsync(context);
    }

    if (app.Configuration.GetValue("Database:RunSeedOnStartup", true))
    {
        await ApplicationDbContextSeeder.SeedAsync(context);
        Log.Information("Database seeding completed.");
    }
}

app.Run();

public partial class Program
{
}

