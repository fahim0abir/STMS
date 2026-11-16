using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

public class ProjectQueryDto
{
    public const string DefaultSortBy = "Name";
    public const string DefaultSortOrder = "asc";

    public static readonly string[] AllowedSortFields = { "Name", "CreatedAt", "StartDate" };

    public string? Keyword { get; init; }
    public ProjectStatus? Status { get; init; }
    public bool IncludeArchived { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; } = DefaultSortOrder;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
