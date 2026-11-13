using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartTask.API;
using SmartTask.Application.DTOs;
using SmartTask.Application.DTOs.Auth;

namespace SmartTask.E2ETests.Infrastructure;

public sealed class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public HttpClient Http => _http;

    public async Task<AuthSession> LoginAsync(string email, string password)
    {
        using var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = password }, Json);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResultDto>>(Json);
        var data = body?.Data ?? throw new InvalidOperationException("Login response did not contain a payload.");
        return new AuthSession(data.AccessToken, data.RefreshToken, data.User);
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        return _http.SendAsync(request);
    }

    public Task<HttpResponseMessage> GetAsync(string path, string? token = null) =>
        SendAsync(HttpMethod.Get, path, token);

    public Task<HttpResponseMessage> PostAsync(string path, object? body, string? token = null) =>
        SendAsync(HttpMethod.Post, path, token, body);

    public Task<HttpResponseMessage> PutAsync(string path, object? body, string? token = null) =>
        SendAsync(HttpMethod.Put, path, token, body);

    public Task<HttpResponseMessage> PatchAsync(string path, object? body, string? token = null) =>
        SendAsync(HttpMethod.Patch, path, token, body);

    public Task<HttpResponseMessage> DeleteAsync(string path, string? token = null) =>
        SendAsync(HttpMethod.Delete, path, token);

    public static async Task<ApiResponse<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json);
        return body ?? throw new InvalidOperationException($"Response from {response.RequestMessage?.RequestUri} had no JSON body.");
    }
}

public record AuthSession(string AccessToken, string RefreshToken, UserDto User);
