using Microsoft.Extensions.Logging;
using ServiceTemplate.Application.Common.Events;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Events;

/// <summary>
/// Example domain event listener — logs when a todo is created.
/// Add side-effects here such as sending notifications, updating projections, or triggering integrations.
/// </summary>
public sealed class TodoCreatedEventListener(ILogger<TodoCreatedEventListener> logger)
    : IDomainEventListener<TodoCreatedEvent>
{
    private static readonly Action<ILogger, Guid, string, Exception?> LogTodoCreated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(100, "TodoCreated"),
            "Todo created — Id: {TodoId}, Title: {Title}");

    public Task HandleAsync(TodoCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        LogTodoCreated(logger, domainEvent.TodoId, domainEvent.Title, null);
        return Task.CompletedTask;
    }
}
