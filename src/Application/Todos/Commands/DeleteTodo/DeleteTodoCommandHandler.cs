using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.DeleteTodo;

public sealed class DeleteTodoCommandHandler(
    ITodoRepository repository,
    IUnitOfWork unitOfWork,
    ITodoMetrics metrics) : IRequestHandler<DeleteTodoCommand, Result<bool>>
{
    public async Task<Result<bool>> HandleAsync(DeleteTodoCommand request, CancellationToken cancellationToken = default)
    {
        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound;
        }

        await repository.DeleteAsync(todo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.RecordDeleted();

        return true;
    }
}
