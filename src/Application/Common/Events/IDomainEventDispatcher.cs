using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Application.Common.Events;

/// <summary>Dispatches collected domain events to their registered handlers.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
