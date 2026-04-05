namespace ServiceTemplate.Domain.Common;

/// <summary>Base class for all domain entities.</summary>
public abstract class Entity
{
    // Version 7 UUIDs are time-sortable, improving PostgreSQL B-tree index performance.
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
