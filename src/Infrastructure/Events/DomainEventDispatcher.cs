using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ServiceTemplate.Application.Common.Events;
using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Infrastructure.Events;

/// <summary>
/// Dispatches domain events to all registered <see cref="IDomainEventHandler{TEvent}"/> instances.
/// Handler MethodInfo is cached per event type to avoid per-dispatch reflection overhead.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, (Type HandlerType, MethodInfo Method)> _cache = new();

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();

            var (handlerType, method) = _cache.GetOrAdd(eventType, t =>
            {
                var ht = typeof(IDomainEventListener<>).MakeGenericType(t);
                var m = ht.GetMethod(nameof(IDomainEventListener<IDomainEvent>.HandleAsync))!;
                return (ht, m);
            });

            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
