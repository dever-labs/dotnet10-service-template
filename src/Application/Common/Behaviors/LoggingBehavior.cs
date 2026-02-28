using Microsoft.Extensions.Logging;
using ServiceTemplate.Application.Common.Cqrs;

namespace ServiceTemplate.Application.Common.Behaviors;

/// <summary>Logs every request/response passing through the pipeline.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", name);

        var response = await next(cancellationToken);

        logger.LogInformation("Handled {RequestName}", name);

        return response;
    }
}
