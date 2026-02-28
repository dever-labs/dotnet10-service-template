using MediatR;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.UpdateTodo;

public sealed class UpdateTodoCommandHandler(
    ITodoRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdateTodoCommand, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound;
        }

        var result = todo.Update(request.Title, request.Description, request.DueDate, timeProvider);

        if (!result.IsSuccess)
        {
            return result.Error!.Value;
        }

        await repository.UpdateAsync(todo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TodoResponse.FromTodo(todo);
    }
}
