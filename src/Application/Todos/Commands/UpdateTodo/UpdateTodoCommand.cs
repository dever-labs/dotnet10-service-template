using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Logging;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.UpdateTodo;

public sealed record UpdateTodoCommand(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueDate) : IRequest<Result<TodoResponse>>, IAuditableRequest
{
    public string EntityType => "Todo";
}
