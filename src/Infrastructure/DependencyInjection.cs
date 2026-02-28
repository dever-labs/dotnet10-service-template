using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Logging;
using ServiceTemplate.Infrastructure.Logging;
using ServiceTemplate.Infrastructure.Persistence;
using ServiceTemplate.Infrastructure.Persistence.Repositories;

namespace ServiceTemplate.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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

        services.Configure<AuditLogOptions>(configuration.GetSection(AuditLogOptions.SectionName));
        services.AddSingleton<IAuditLogger, SyslogAuditLogger>();

        return services;
    }
}
