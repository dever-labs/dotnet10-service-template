using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Logging;
using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Application.Todos.Commands.DeleteTodo;

public sealed record DeleteTodoCommand(Guid Id) : IRequest<Result<bool>>, IAuditableRequest
{
    public string EntityType => "Todo";
}
