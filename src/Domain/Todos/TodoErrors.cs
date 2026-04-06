using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Domain.Todos;

public static class TodoErrors
{
    public static readonly DomainError TitleRequired = DomainError.Validation("Todo.TitleRequired", "Title is required.");
    public static readonly DomainError TitleTooLong = DomainError.Validation("Todo.TitleTooLong", "Title must not exceed 200 characters.");
    public static readonly DomainError NotFound = DomainError.NotFound("Todo.NotFound", "The requested todo was not found.");
    public static readonly DomainError AlreadyCompleted = DomainError.Conflict("Todo.AlreadyCompleted", "This todo has already been completed.");
}
