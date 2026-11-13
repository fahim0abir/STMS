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

builder.Services.AddScoped<IRepository<SmartTask.Domain.Entities.User>, Repository<SmartTask.Domain.Entities.User>>();
builder.Services.AddScoped<IRepository<SmartTask.Domain.Entities.Project>, Repository<SmartTask.Domain.Entities.Project>>();
builder.Services.AddScoped<IRepository<SmartTask.Domain.Entities.TaskItem>, Repository<SmartTask.Domain.Entities.TaskItem>>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, SmartTask.Infrastructure.Services.ProjectService>();
builder.Services.AddScoped<ITaskItemService, SmartTask.Infrastructure.Services.TaskItemService>();
builder.Services.AddScoped<IDashboardService, SmartTask.Infrastructure.Services.DashboardService>();
builder.Services.AddScoped<ITaskDescriptionEnhancementService, SmartTask.Infrastructure.Services.TaskDescriptionEnhancementService>();
builder.Services.AddScoped<IValidator<ProjectQueryDto>, ProjectQueryDtoValidator>();
builder.Services.AddScoped<IValidator<TaskItemQueryDto>, TaskItemQueryDtoValidator>();
builder.Services.AddScoped<IValidator<EnhanceDescriptionRequestDto>, EnhanceDescriptionRequestDtoValidator>();

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await ApplicationDbContextSeeder.SeedAsync(context);
}

app.Run();

public partial class Program
{
}

