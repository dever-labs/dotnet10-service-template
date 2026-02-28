using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Application.Common.Telemetry;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.CreateTodo;

public sealed class CreateTodoCommandHandler(
    ITodoRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ITodoMetrics metrics) : IRequestHandler<CreateTodoCommand, Result<TodoResponse>>
{
    public async Task<Result<TodoResponse>> HandleAsync(CreateTodoCommand request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = Todo.Create(request.Title, request.Description, request.DueDate, timeProvider);

        if (!result.IsSuccess)
        {
            return result.Error!.Value;
        }

        await repository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        metrics.RecordCreated();

        return TodoResponse.FromTodo(result.Value);
    }
}
