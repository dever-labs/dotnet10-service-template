namespace ServiceTemplate.Application.Common.Cqrs;

/// <summary>Handles a request of type <typeparamref name="TRequest"/>.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
