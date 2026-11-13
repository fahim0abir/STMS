namespace SmartTask.Application.DTOs;

public class ProjectQueryDto
{
    public const string DefaultSortBy = "Name";
    public const string DefaultSortOrder = "asc";

    public static readonly string[] AllowedSortFields = { "Name", "CreatedAt" };

    public string? Keyword { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; } = DefaultSortOrder;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
