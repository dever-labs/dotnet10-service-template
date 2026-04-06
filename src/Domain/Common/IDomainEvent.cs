namespace ServiceTemplate.Domain.Common;

/// <summary>Marker interface for domain events.</summary>
#pragma warning disable CA1040 // Marker interfaces are a valid DDD pattern; conversion to attribute would break dispatch conventions
public interface IDomainEvent;
#pragma warning restore CA1040
