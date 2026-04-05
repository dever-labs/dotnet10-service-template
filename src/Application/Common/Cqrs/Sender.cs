using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceTemplate.Application.Common.Cqrs;

/// <summary>
/// Resolves the handler for a request via DI, then executes it through the registered pipeline behaviors.
/// MethodInfo objects are cached per type to avoid repeated reflection lookups on the hot path.
/// </summary>
public sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _handlerMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> _behaviorMethodCache = new();

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = serviceProvider.GetRequiredService(handlerType);

        // Collect behaviors registered for this request/response pair
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = serviceProvider.GetServices(behaviorType).ToList();

        // Cache GetMethod lookups — GetMethod is cheap but called on every request
        var handlerHandleMethod = _handlerMethodCache.GetOrAdd(handlerType,
            t => t.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!);

        var behaviorHandleMethod = _behaviorMethodCache.GetOrAdd(behaviorType,
            t => t.GetMethod(nameof(IPipelineBehavior<IRequest<TResponse>, TResponse>.HandleAsync))!);

        // Build the pipeline by wrapping from the innermost handler outward
        RequestHandlerFunc<TResponse> pipeline = ct =>
            (Task<TResponse>)handlerHandleMethod.Invoke(handler, [request, ct])!;

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i]!;
            var currentNext = pipeline;
            pipeline = ct =>
                (Task<TResponse>)behaviorHandleMethod.Invoke(behavior, [request, currentNext, ct])!;
        }

        return pipeline(cancellationToken);
    }
}
