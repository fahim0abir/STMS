using System.Net;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Enums;
using SmartTask.E2ETests.Infrastructure;

namespace SmartTask.E2ETests.Users;

[Collection(E2ECollection.Name)]
public class UserEmailUniquenessE2ETests
{
    private readonly E2EFixture _fixture;

    public UserEmailUniquenessE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUser_WithCaseVariantExistingEmail_ReturnsBadRequest()
    {
        var response = await _fixture.Api.PostAsync(
            "/api/users",
            new
            {
                Email = $"  {TestData.MemberEmail.ToUpperInvariant()} ",
                Password = "Passw0rd!",
                Name = "Duplicate Member",
                Role = Role.TeamMember
            },
            _fixture.Admin.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiClient.ReadAsync<UserDto>(response);
        Assert.False(body.Success);
        Assert.Contains("already exists", body.Message, StringComparison.OrdinalIgnoreCase);
    }
}
