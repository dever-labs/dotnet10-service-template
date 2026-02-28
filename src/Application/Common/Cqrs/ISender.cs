namespace ServiceTemplate.Application.Common.Cqrs;

/// <summary>Dispatches requests to their registered handler, passing through any pipeline behaviors.</summary>
public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
