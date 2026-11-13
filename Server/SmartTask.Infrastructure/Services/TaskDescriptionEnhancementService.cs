using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartTask.Infrastructure.Services;

public class TaskDescriptionEnhancementService : ITaskDescriptionEnhancementService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private const string Model = "gpt-4o-mini";
    private const string Endpoint = "https://models.inference.ai.azure.com/chat/completions";

    private const string SystemPrompt = """
        You are a professional task-description editor. Improve the given task description for
        grammar, clarity, and a professional tone. Return ONLY the enhanced description as
        actionable bullet points. Do not add preamble, explanations, or extra sections.
        """;

    public TaskDescriptionEnhancementService(IConfiguration configuration)
    {
        _apiKey = configuration["AI:GitHubModels:ApiKey"];
        _httpClient = new HttpClient { BaseAddress = new Uri(Endpoint), Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> EnhanceAsync(string description)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length is < 5 or > 1000)
            return description;

        if (string.IsNullOrWhiteSpace(_apiKey))
            return FallbackEnhance(description);

        try
        {
            var request = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = description }
                },
                temperature = 0.7,
                max_tokens = 500
            };

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            using var response = await _httpClient.PostAsync(string.Empty, content);
            if (!response.IsSuccessStatusCode)
                return FallbackEnhance(description);

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var enhanced = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(enhanced) ? FallbackEnhance(description) : enhanced.Trim();
        }
        catch
        {
            return FallbackEnhance(description);
        }
    }

    private static string FallbackEnhance(string description)
    {
        var trimmed = description.Trim();
        if (!trimmed.EndsWith('.')) trimmed += '.';
        return $"- {trimmed}";
    }
}
