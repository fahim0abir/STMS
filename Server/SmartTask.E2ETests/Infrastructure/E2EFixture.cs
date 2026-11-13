using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartTask.E2ETests.Infrastructure;

[CollectionDefinition(Name)]
public class E2ECollection : ICollectionFixture<E2EFixture>
{
    public const string Name = "e2e";
}

public sealed class E2EFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public ApiClient Api { get; private set; } = null!;
    public AuthSession Admin { get; private set; } = null!;
    public AuthSession Manager { get; private set; } = null!;
    public AuthSession Member { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory();
        Api = new ApiClient(Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));

        await TestDataSeeder.SeedAsync(Factory.Services);

        Admin = await Api.LoginAsync(TestData.AdminEmail, TestData.AdminPassword);
        Manager = await Api.LoginAsync(TestData.ManagerEmail, TestData.ManagerPassword);
        Member = await Api.LoginAsync(TestData.MemberEmail, TestData.MemberPassword);
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
