using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Infrastructure.AI;
using ServiceTemplate.Infrastructure.Persistence;
using ServiceTemplate.Infrastructure.Persistence.Repositories;

namespace ServiceTemplate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                npgsql.CommandTimeout(30);
            }));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITodoRepository, TodoRepository>();

        services.Configure<GitHubModelsOptions>(
            configuration.GetSection(GitHubModelsOptions.SectionName));
        services.AddHttpClient<GitHubModelsClient>();

        return services;
    }
}
