using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceTemplate.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ServiceTemplate.AcceptanceTests.Fixtures;

/// <summary>
/// Acceptance test fixture — starts the full application stack against a real database.
/// Tests are written in a BDD style (Given / When / Then) to document business requirements.
/// </summary>
public sealed class AcceptanceTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("acceptance_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Test");
                host.ConfigureAppConfiguration(cfg =>
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        // Override connection string so the NpgSql health check and EF Core
                        // both target the Testcontainers instance, not localhost:5432
                        ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                    }));
                host.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<AppDbContext>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString()));
                });
            });

        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition(nameof(AcceptanceTestCollection))]
public sealed class AcceptanceTestCollection : ICollectionFixture<AcceptanceTestFixture>;
