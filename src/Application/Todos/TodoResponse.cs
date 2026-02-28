using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Application.Todos;

/// <summary>DTO returned for Todo read operations.</summary>
public sealed record TodoResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TodoResponse FromTodo(Todo todo) =>
        new(todo.Id, todo.Title, todo.Description, todo.Status.ToString(), todo.DueDate, todo.CreatedAt, todo.UpdatedAt);
}
