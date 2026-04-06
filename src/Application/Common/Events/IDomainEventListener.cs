using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Application.Common.Events;

/// <summary>Handles a specific domain event type in-process.</summary>
public interface IDomainEventListener<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
