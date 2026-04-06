using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Logging;
using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Application.Todos.Commands.CompleteTodo;

public sealed record CompleteTodoCommand(Guid Id) : IRequest<Result<TodoResponse>>, IAuditableRequest
{
    public string EntityType => "Todo";
}
