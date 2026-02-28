using MediatR;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.DeleteTodo;

public sealed class DeleteTodoCommandHandler(
    ITodoRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteTodoCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound;
        }

        await repository.DeleteAsync(todo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
