using Microsoft.Extensions.DependencyInjection;

namespace ServiceTemplate.Application.Common.Cqrs;

/// <summary>
/// Resolves the handler for a request via DI, then executes it through the registered pipeline behaviors.
/// </summary>
public sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = serviceProvider.GetRequiredService(handlerType);

        // Collect behaviors registered for this request/response pair
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = serviceProvider.GetServices(behaviorType).ToList();

        // Build the pipeline by wrapping from the innermost handler outward
        RequestHandlerDelegate<TResponse> pipeline = ct =>
        {
            var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!;
            return (Task<TResponse>)handleMethod.Invoke(handler, [request, ct])!;
        };

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i]!;
            var currentNext = pipeline;
            pipeline = ct =>
            {
                var handleMethod = behaviorType.GetMethod(nameof(IPipelineBehavior<IRequest<TResponse>, TResponse>.HandleAsync))!;
                return (Task<TResponse>)handleMethod.Invoke(behavior, [request, currentNext, ct])!;
            };
        }

        return pipeline(cancellationToken);
    }
}
