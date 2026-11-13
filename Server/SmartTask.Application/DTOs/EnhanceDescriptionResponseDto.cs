using SmartTask.Domain.Enums;

namespace SmartTask.Application.DTOs;

/// <summary>
/// Structured result of an AI task-description enhancement.
/// </summary>
public record EnhanceDescriptionResponseDto
{
    public string EnhancedDescription { get; init; } = string.Empty;
    public string SuggestedTitle { get; init; } = string.Empty;
    public IReadOnlyList<string> AcceptanceCriteria { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Subtasks { get; init; } = Array.Empty<string>();
    public TaskPriority SuggestedPriority { get; init; } = TaskPriority.Medium;
    public decimal? EstimatedHours { get; init; }
    public string Model { get; init; } = "fallback";
    public bool UsedFallback { get; init; }
}
