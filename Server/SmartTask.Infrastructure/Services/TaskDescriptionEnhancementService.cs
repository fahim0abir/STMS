using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using SmartTask.Domain.Enums;
using Google.GenAI;
using Google.GenAI.Types;

namespace SmartTask.Infrastructure.Services;

/// <summary>
/// Rewrites rough task notes into a structured, production-ready task definition using Google AI Studio
/// (Gemini) through the official Google.GenAI SDK, with a deterministic fallback when the model is unavailable.
/// </summary>
public class TaskDescriptionEnhancementService : ITaskDescriptionEnhancementService
{
    private const string DefaultModel = "gemini-3.1-flash-lite";
    private const string PlaceholderPrefix = "REPLACE_WITH";
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(20);
    private const int MaxAttempts = 2;
    private const int MaxListItems = 8;
    private const int MaxDescriptionLength = 1200;
    private const int MaxListItemLength = 240;

    private const string SystemPrompt = """
        You are a senior delivery lead who turns rough engineering notes into precise, actionable work items.
        Return a single JSON object and nothing else. No markdown fences, no commentary.

        Schema:
        {
          "enhancedDescription": "string",
          "suggestedTitle": "string",
          "acceptanceCriteria": ["string"],
          "subtasks": ["string"],
          "suggestedPriority": "Low" | "Medium" | "High" | "Critical",
          "estimatedHours": number | null
        }

        Rules:
        - Fix grammar and spelling; keep the author's intent and technical vocabulary.
        - enhancedDescription: professional tone, at most 1200 characters, structured as short sentences or hyphen bullets.
        - suggestedTitle: imperative mood, at most 90 characters, no trailing period.
        - acceptanceCriteria: 3 to 6 verifiable statements (testable, observable outcomes).
        - subtasks: 2 to 6 concrete implementation steps, each starting with a verb.
        - suggestedPriority: infer from impact, urgency words and scope; default to Medium when unclear.
        - estimatedHours: realistic effort in hours (0.5 to 200) or null when the scope is unclear.
        - Never invent external systems, credentials, or requirements that are not implied by the input.
        """;

    private readonly Client? _client;
    private readonly ILogger<TaskDescriptionEnhancementService> _logger;
    private readonly bool _isConfigured;
    private readonly string _model;
    private readonly double _temperature;

    public TaskDescriptionEnhancementService(IConfiguration configuration, ILogger<TaskDescriptionEnhancementService> logger)
    {
        _logger = logger;

        // Google AI Studio keys live in user-secrets. A missing or placeholder key disables the provider
        // entirely so the deterministic fallback is used without a network round-trip.
        var apiKey = configuration["AI:GoogleAIStudio:ApiKey"];
        _isConfigured = !string.IsNullOrWhiteSpace(apiKey)
            && !apiKey.StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase);
        _model = configuration["AI:GoogleAIStudio:Model"] is { Length: > 0 } model ? model : DefaultModel;
        _temperature = double.TryParse(configuration["AI:GoogleAIStudio:Temperature"], NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature)
            ? Math.Clamp(temperature, 0, 1)
            : 0.4;

        if (!_isConfigured)
        {
            return;
        }

        try
        {
            _client = new Client(apiKey: apiKey);
        }
        catch (Exception exception)
        {
            _isConfigured = false;
            _logger.LogWarning(exception, "Google AI Studio client could not be created; returning the deterministic fallback.");
        }
    }

    public async Task<EnhanceDescriptionResponseDto> EnhanceAsync(EnhanceDescriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        var description = (request.Description ?? string.Empty).Trim();
        if (description.Length < 20 || description.Length > 4000)
        {
            return BuildFallback(request);
        }

        if (!_isConfigured)
        {
            _logger.LogWarning("AI provider API key is missing or a placeholder; returning the deterministic fallback.");
            return BuildFallback(request);
        }

        var userContent = BuildUserContent(request, description);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var raw = await CallModelAsync(userContent, cancellationToken);
                var parsed = TryParse(raw);
                if (parsed is not null)
                {
                    _logger.LogInformation("AI enhancement succeeded with {Model} on attempt {Attempt}.", _model, attempt);
                    return parsed;
                }

                _logger.LogWarning("AI provider response could not be parsed on attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(exception, "AI provider call timed out on attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "AI provider call failed on attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);
            }

            if (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
            }
        }

        _logger.LogWarning("AI enhancement fell back to the local generator after {MaxAttempts} attempt(s).", MaxAttempts);
        return BuildFallback(request);
    }

    private static string BuildUserContent(EnhanceDescriptionRequestDto request, string description)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Rewrite the following task note into the required JSON schema.");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            builder.AppendLine($"Working title: {request.Title.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            builder.AppendLine($"Project / domain context: {request.Context.Trim()}");
        }

        builder.AppendLine("Task note:");
        builder.AppendLine(description);
        return builder.ToString();
    }

    private async Task<string> CallModelAsync(string userContent, CancellationToken cancellationToken)
    {
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = new List<Part> { new() { Text = SystemPrompt } }
            },
            Temperature = (float)_temperature,
            MaxOutputTokens = 900,
            ResponseMimeType = "application/json"
        };

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ProviderTimeout);

        var response = await _client!.Models.GenerateContentAsync(
            model: _model,
            contents: userContent,
            config: config,
            cancellationToken: timeoutSource.Token);

        var text = response.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("The AI provider returned an empty completion.");
        }

        return text;
    }

    private EnhanceDescriptionResponseDto? TryParse(string rawResponse)
    {
        var json = ExtractJsonObject(rawResponse);
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return Map(document.RootElement);
    }

    private EnhanceDescriptionResponseDto? Map(JsonElement root)
    {
        var enhanced = Sanitize(ReadString(root, "enhancedDescription"));
        if (string.IsNullOrWhiteSpace(enhanced))
        {
            return null;
        }

        return new EnhanceDescriptionResponseDto
        {
            EnhancedDescription = Truncate(enhanced, MaxDescriptionLength),
            SuggestedTitle = Truncate(Sanitize(ReadString(root, "suggestedTitle")), 90),
            AcceptanceCriteria = ReadList(root, "acceptanceCriteria"),
            Subtasks = ReadList(root, "subtasks"),
            SuggestedPriority = ParsePriority(ReadString(root, "suggestedPriority")),
            EstimatedHours = ReadDecimal(root, "estimatedHours"),
            Model = _model,
            UsedFallback = false
        };
    }

    private static EnhanceDescriptionResponseDto BuildFallback(EnhanceDescriptionRequestDto request)
    {
        var description = (request.Description ?? string.Empty).Trim();
        var sentences = description
            .Split(new[] { '.', '!', '?', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(sentence => sentence.Length > 3)
            .Take(MaxListItems)
            .ToArray();

        var body = sentences.Length > 0
            ? sentences.Select(sentence => $"- {sentence.Trim().TrimEnd('.')}.")
            : new[] { $"- {description}." };

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? Truncate(string.Join(' ', description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(12)), 90)
            : Truncate(request.Title.Trim(), 90);

        var criteria = new[]
        {
            $"Implemented behaviour matches the task note: {Truncate(sentences.FirstOrDefault() ?? description, MaxListItemLength)}",
            "Edge cases and validation rules are covered and verified manually.",
            "Changes are reviewed and documented before the task is marked Completed."
        };

        var subtasks = sentences.Length > 1
            ? sentences.Skip(1).Select(sentence => Truncate(sentence, MaxListItemLength)).ToArray()
            : new[] { "Break the description into verifiable steps.", "Implement and verify the change end to end." };

        return new EnhanceDescriptionResponseDto
        {
            EnhancedDescription = Truncate(string.Join(System.Environment.NewLine, body), MaxDescriptionLength),
            SuggestedTitle = title,
            AcceptanceCriteria = criteria,
            Subtasks = subtasks,
            SuggestedPriority = InferPriority(description),
            EstimatedHours = null,
            Model = "fallback",
            UsedFallback = true
        };
    }

    private static TaskPriority InferPriority(string description)
    {
        var text = description.ToLowerInvariant();
        if (text.Contains("security") || text.Contains("down") || text.Contains("urgent") || text.Contains("asap"))
        {
            return TaskPriority.Critical;
        }

        if (text.Contains("bug") || text.Contains("error") || text.Contains("fail"))
        {
            return TaskPriority.High;
        }

        return TaskPriority.Medium;
    }

    private static TaskPriority ParsePriority(string? value) =>
        Enum.TryParse<TaskPriority>(value, ignoreCase: true, out var priority) ? priority : TaskPriority.Medium;

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static decimal? ReadDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.TryGetDecimal(out var value) ? Math.Clamp(value, 0.5m, 200m) : null;
    }

    private static IReadOnlyList<string> ReadList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => Truncate(Sanitize(item.GetString()), MaxListItemLength))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(MaxListItems)
            .ToArray();
    }

    private static string? ExtractJsonObject(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var cleaned = payload.Replace("```json", string.Empty).Replace("```", string.Empty).Trim();
        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return cleaned.Substring(start, end - start + 1);
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value
            .Replace("\r", " ")
            .Replace("**", string.Empty)
            .Replace("__", string.Empty)
            .Trim();

        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
