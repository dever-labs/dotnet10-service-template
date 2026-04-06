using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.CompleteTodo;

public sealed class CompleteTodoCommandHandler(
    ITodoRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ITodoMetrics metrics) : IRequestHandler<CompleteTodoCommand, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> HandleAsync(CompleteTodoCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todo = await repository.GetByIdAsync(request.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound;
        }

        var result = todo.Complete(timeProvider);

        if (!result.IsSuccess)
        {
            return result.Error!.Value;
        }

        await repository.UpdateAsync(todo, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.RecordCompleted();

        return TodoResponse.FromTodo(todo);
    }
}
