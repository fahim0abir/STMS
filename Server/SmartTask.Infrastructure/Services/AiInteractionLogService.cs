using Microsoft.EntityFrameworkCore;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Entities;
using SmartTask.Infrastructure.Repositories;

namespace SmartTask.Infrastructure.Services;

/// <summary>
/// Persists successful AI provider calls and exposes them to administrators as a paged audit list.
/// </summary>
public class AiInteractionLogService : IAiInteractionLogService
{
    private readonly IRepository<AiInteractionLog> _repository;

    public AiInteractionLogService(IRepository<AiInteractionLog> repository)
    {
        _repository = repository;
    }

    public async Task LogSuccessAsync(AiInteractionLogCreateDto log, CancellationToken cancellationToken = default)
    {
        var entity = new AiInteractionLog
        {
            Id = Guid.NewGuid(),
            UserId = log.UserId,
            UserEmail = log.UserEmail,
            UserName = log.UserName,
            UserRole = log.UserRole,
            TaskId = log.TaskId,
            Model = log.Model,
            Prompt = log.Prompt,
            InputText = log.InputText,
            OutputJson = log.OutputJson,
            DurationMs = log.DurationMs,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task<PagedResult<AiInteractionLogDto>> GetPagedAsync(AiInteractionLogQueryDto query, CancellationToken cancellationToken = default)
    {
        var logs = _repository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            logs = logs.Where(x =>
                x.Prompt.Contains(keyword)
                || x.OutputJson.Contains(keyword)
                || x.UserEmail.Contains(keyword)
                || (x.UserName != null && x.UserName.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(query.Model))
        {
            var model = query.Model.Trim();
            logs = logs.Where(x => x.Model == model);
        }

        if (query.UserId.HasValue) logs = logs.Where(x => x.UserId == query.UserId.Value);
        if (query.From.HasValue) logs = logs.Where(x => x.CreatedAt >= query.From.Value);
        if (query.To.HasValue) logs = logs.Where(x => x.CreatedAt <= query.To.Value);

        var projected = logs
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AiInteractionLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserEmail = x.UserEmail,
                UserName = x.UserName,
                UserRole = x.UserRole,
                TaskId = x.TaskId,
                Model = x.Model,
                Prompt = x.Prompt,
                InputText = x.InputText,
                OutputJson = x.OutputJson,
                DurationMs = x.DurationMs,
                CreatedAt = x.CreatedAt
            });

        return await projected.ToPagedResultAsync(query.PageNumber, query.PageSize, cancellationToken);
    }
}
