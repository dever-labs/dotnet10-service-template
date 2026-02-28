using MediatR;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Queries.GetTodo;

public sealed class GetTodoQueryHandler(ITodoRepository repository) : IRequestHandler<GetTodoQuery, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> Handle(GetTodoQuery request, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound;
        }

        return TodoResponse.FromTodo(todo);
    }
}
