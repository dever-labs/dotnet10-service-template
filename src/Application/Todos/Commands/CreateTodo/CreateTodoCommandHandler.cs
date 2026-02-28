using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.CreateTodo;

public sealed class CreateTodoCommandHandler(
    ITodoRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreateTodoCommand, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> HandleAsync(CreateTodoCommand request, CancellationToken cancellationToken = default)
    {
        var result = Todo.Create(request.Title, request.Description, request.DueDate, timeProvider);

        if (!result.IsSuccess)
        {
            return result.Error!.Value;
        }

        await repository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TodoResponse.FromTodo(result.Value);
    }
}
