namespace ServiceTemplate.Application.Common.Cqrs;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>Wraps handler execution; implement to add cross-cutting behaviour (logging, validation, etc.).</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}
