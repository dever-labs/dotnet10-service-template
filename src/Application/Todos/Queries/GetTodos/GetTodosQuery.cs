using MediatR;
using ServiceTemplate.Application.Common.Models;

namespace ServiceTemplate.Application.Todos.Queries.GetTodos;

public sealed record GetTodosQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<TodoResponse>>;
