using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartTask.Infrastructure.Persistence;

namespace SmartTask.E2ETests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"SmartTaskE2E_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=SmartTaskE2E_Unused;Trusted_Connection=True;MultipleActiveResultSets=true",
                ["Jwt:Key"] = "E2E_TESTS_ONLY_SIGNING_KEY_0123456789_ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                ["Jwt:Issuer"] = "SmartTask",
                ["Jwt:Audience"] = "SmartTaskClients",
                ["RateLimiting:Default"] = "10000",
                ["RateLimiting:Auth"] = "10000",
                ["RateLimiting:Enhance"] = "10000",
                ["AI:GoogleAIStudio:ApiKey"] = ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
