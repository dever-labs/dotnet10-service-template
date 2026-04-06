namespace ServiceTemplate.Application.Common.Cqrs;

/// <summary>Marker interface for a request that returns <typeparamref name="TResponse"/>.</summary>
#pragma warning disable CA1040 // Marker interface is intentional — enables open-generic pipeline registration without a heavy framework dependency
public interface IRequest<TResponse>;
#pragma warning restore CA1040
