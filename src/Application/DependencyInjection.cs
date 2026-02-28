using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Behaviors;
using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Telemetry;

namespace ServiceTemplate.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register our custom CQRS dispatcher
        services.AddScoped<ISender, Sender>();

        // Register all request handlers from this assembly
        var assembly = typeof(ApplicationServiceExtensions).Assembly;
        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var iface in type.GetInterfaces().Where(i =>
                         i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            {
                services.AddScoped(iface, type);
            }
        }

        // Register open generic pipeline behaviors (applied in registration order)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        // FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Custom metrics — exported via OTel; meter registered in Program.cs
        services.AddSingleton<ITodoMetrics, TodoMetrics>();

        return services;
    }
}
