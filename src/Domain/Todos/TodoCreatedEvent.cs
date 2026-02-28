using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Domain.Todos;

public sealed record TodoCreatedEvent(Guid TodoId, string Title) : IDomainEvent;
