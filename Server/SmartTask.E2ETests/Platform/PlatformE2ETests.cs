using System.Net;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Platform;

[Collection(E2ECollection.Name)]
public class PlatformE2ETests
{
    private readonly E2EFixture _fixture;

    public PlatformE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _fixture.Api.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EnhanceDescription_WithoutAiKey_UsesDeterministicFallback()
    {
        var response = await _fixture.Api.PostAsync(
            "/api/tasks/enhance-description",
            new { Description = "fix login page failing when password has spaces", Title = "Fix login", Context = "SmartTask auth module" },
            _fixture.Member.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<EnhanceDescriptionResponseDto>(response);
        var result = body.Data!;

        Assert.True(body.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal("fallback", result.Model);
        Assert.False(string.IsNullOrWhiteSpace(result.EnhancedDescription));
        Assert.False(string.IsNullOrWhiteSpace(result.SuggestedTitle));
        Assert.NotEmpty(result.AcceptanceCriteria);
        Assert.NotEmpty(result.Subtasks);
        Assert.Equal(TaskPriority.High, result.SuggestedPriority);
    }

    [Theory]
    [InlineData("too short")]
    [InlineData("")]
    public async Task EnhanceDescription_WithInvalidLength_Returns400(string description)
    {
        var response = await _fixture.Api.PostAsync(
            "/api/tasks/enhance-description",
            new { Description = description },
            _fixture.Member.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiClient.ReadAsync<EnhanceDescriptionResponseDto>(response);
        Assert.False(body.Success);
    }
}
