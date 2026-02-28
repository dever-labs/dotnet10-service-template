using ServiceTemplate.Application.Common.Cqrs;
using ServiceTemplate.Domain.Common;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos.Commands.CreateTodo;

public sealed record CreateTodoCommand(
    string Title,
    string? Description,
    DateTimeOffset? DueDate) : IRequest<Result<TodoResponse>>;
