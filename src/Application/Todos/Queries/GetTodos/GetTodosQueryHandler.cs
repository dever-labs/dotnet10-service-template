using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Models;

namespace ServiceTemplate.Application.Todos.Queries.GetTodos;

public sealed class GetTodosQueryHandler(ITodoRepository repository) : IRequestHandler<GetTodosQuery, PagedResult<TodoResponse>>
{
    public async Task<PagedResult<TodoResponse>> HandleAsync(GetTodosQuery request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await repository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);

        var responses = items.Select(TodoResponse.FromTodo).ToList();

        return new PagedResult<TodoResponse>(responses, total, request.Page, request.PageSize);
    }
}
