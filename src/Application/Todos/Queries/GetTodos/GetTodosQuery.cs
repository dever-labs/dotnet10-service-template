using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Models;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Queries.GetTodos;

public sealed record GetTodosQuery(int Page = 1, int PageSize = 20, TodoStatus? Status = null) : IRequest<PagedResult<TodoResponse>>;
