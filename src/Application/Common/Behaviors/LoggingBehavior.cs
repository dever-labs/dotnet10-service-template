using Microsoft.Extensions.Logging;
using ServiceTemplate.Application.Common.Cqrs;

namespace ServiceTemplate.Application.Common.Behaviors;

/// <summary>Logs every request/response passing through the pipeline.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly Action<ILogger, string, Exception?> LogHandling =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "Handling"), "Handling {RequestName}");

    private static readonly Action<ILogger, string, Exception?> LogHandled =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, "Handled"), "Handled {RequestName}");

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerFunc<TResponse> nextHandler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nextHandler);

        var name = typeof(TRequest).Name;
        LogHandling(logger, name, null);

        var response = await nextHandler(cancellationToken);

        LogHandled(logger, name, null);

        return response;
    }
}
