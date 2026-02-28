using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Behaviors;
using ServiceTemplate.Application.Common.Cqrs;

namespace ServiceTemplate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register our custom CQRS dispatcher
        services.AddScoped<ISender, Sender>();

        // Register all request handlers from this assembly
        var assembly = typeof(DependencyInjection).Assembly;
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

        // FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
