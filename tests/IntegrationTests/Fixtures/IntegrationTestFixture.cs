using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using ServiceTemplate.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ServiceTemplate.IntegrationTests.Fixtures;

/// <summary>
/// Shared fixture that spins up a PostgreSQL Testcontainer once for the entire test collection,
/// and resets the database state between individual tests using Respawn.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Test");
                host.ConfigureServices(services =>
                {
                    // Replace the DbContext to use the test container
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<AppDbContext>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString()));
                });
            });

        Client = Factory.CreateClient();

        // Apply migrations
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        // Respawn requires an open DbConnection for non-SQL Server adapters
        await using var conn = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
