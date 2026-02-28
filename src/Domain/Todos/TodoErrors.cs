using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Domain.Todos;

public static class TodoErrors
{
    public static readonly Error TitleRequired = Error.Validation("Todo.TitleRequired", "Title is required.");
    public static readonly Error TitleTooLong = Error.Validation("Todo.TitleTooLong", "Title must not exceed 200 characters.");
    public static readonly Error NotFound = Error.NotFound("Todo.NotFound", "The requested todo was not found.");
    public static readonly Error AlreadyCompleted = Error.Conflict("Todo.AlreadyCompleted", "This todo has already been completed.");
}
