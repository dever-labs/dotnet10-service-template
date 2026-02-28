using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Logging;

namespace ServiceTemplate.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that writes an RFC 5424 audit log entry after every
/// <see cref="IAuditableRequest"/> completes. Runs after validation so
/// invalid requests are never audited.
/// Extracts outcome from <c>Result&lt;T&gt;</c> responses automatically.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditLogger auditLogger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var response = await next(cancellationToken);

        if (request is not IAuditableRequest auditable)
        {
            return response;
        }

        var action = typeof(TRequest).Name.Replace("Command", string.Empty, StringComparison.Ordinal);
        var (outcome, entityId, errorCode) = ExtractResult(response);

        auditLogger.Log(new AuditEvent(action, auditable.EntityType, entityId, outcome, errorCode));

        return response;
    }

    /// <summary>
    /// Reads <c>IsSuccess</c>, <c>Value.Id</c>, and <c>Error.Code</c> from
    /// <c>Result&lt;T&gt;</c> via reflection. Falls back gracefully for other return types.
    /// </summary>
    private static (AuditOutcome Outcome, string? EntityId, string? ErrorCode) ExtractResult(TResponse response)
    {
        if (response is null)
        {
            return (AuditOutcome.Failure, null, "NullResponse");
        }

        var type = typeof(TResponse);
        var isSuccessProp = type.GetProperty("IsSuccess");

        if (isSuccessProp is null)
        {
            // Not a Result<T> — treat as success
            return (AuditOutcome.Success, null, null);
        }

        var isSuccess = isSuccessProp.GetValue(response) is true;

        string? entityId = null;
        if (isSuccess)
        {
            var value = type.GetProperty("Value")?.GetValue(response);
            entityId = value?.GetType().GetProperty("Id")?.GetValue(value)?.ToString();
        }

        string? errorCode = null;
        if (!isSuccess)
        {
            var error = type.GetProperty("Error")?.GetValue(response);
            errorCode = error?.GetType().GetProperty("Code")?.GetValue(error)?.ToString();
        }

        return (isSuccess ? AuditOutcome.Success : AuditOutcome.Failure, entityId, errorCode);
    }
}
