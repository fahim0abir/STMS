using System.Net;
using SmartTask.Application.DTOs;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Auth;

[Collection(E2ECollection.Name)]
public class EmailUniquenessE2ETests
{
    private readonly E2EFixture _fixture;

    public EmailUniquenessE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EmailAvailability_ForExistingEmail_IsCaseInsensitive()
    {
        var response = await _fixture.Api.GetAsync(
            $"/api/auth/email-availability?email={Uri.EscapeDataString(TestData.AdminEmail.ToUpperInvariant())}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<EmailAvailabilityDto>(response);
        Assert.True(body.Success);
        Assert.False(body.Data!.Available);
    }

    [Fact]
    public async Task EmailAvailability_ForUnusedEmail_ReturnsAvailable()
    {
        var email = $"free.{Guid.NewGuid():N}@smarttask.dev";

        var response = await _fixture.Api.GetAsync($"/api/auth/email-availability?email={Uri.EscapeDataString(email)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiClient.ReadAsync<EmailAvailabilityDto>(response);
        Assert.True(body.Data!.Available);
    }

    [Fact]
    public async Task Register_WithCaseVariantExistingEmail_IsRejected()
    {
        var response = await _fixture.Api.PostAsync("/api/auth/register", new
        {
            Email = $"  {TestData.AdminEmail.ToUpperInvariant()} ",
            Password = "Passw0rd!23",
            Name = "Duplicate Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiClient.ReadAsync<object>(response);
        Assert.False(body.Success);
        Assert.Contains("already exists", body.Message, StringComparison.OrdinalIgnoreCase);
    }
}
