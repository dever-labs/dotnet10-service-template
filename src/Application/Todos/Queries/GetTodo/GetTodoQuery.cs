using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Application.Todos.Queries.GetTodo;

public sealed record GetTodoQuery(Guid Id) : IRequest<Result<TodoResponse>>;
