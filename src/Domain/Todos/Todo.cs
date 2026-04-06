using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Domain.Todos;

/// <summary>The Todo aggregate root.</summary>
public sealed class Todo : Entity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TodoStatus Status { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    // EF Core constructor
    private Todo() { }

    public static Result<Todo> Create(string title, string? description, DateTimeOffset? dueDate, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (string.IsNullOrWhiteSpace(title))
        {
            return TodoErrors.TitleRequired;
        }

        if (title.Length > 200)
        {
            return TodoErrors.TitleTooLong;
        }

        var now = timeProvider.GetUtcNow();

        var todo = new Todo
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = TodoStatus.Open,
            DueDate = dueDate,
            CreatedAt = now,
            UpdatedAt = now,
        };

        todo.AddDomainEvent(new TodoCreatedEvent(todo.Id, todo.Title));

        return todo;
    }

    public Result<Todo> Update(string title, string? description, DateTimeOffset? dueDate, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (string.IsNullOrWhiteSpace(title))
        {
            return TodoErrors.TitleRequired;
        }

        if (title.Length > 200)
        {
            return TodoErrors.TitleTooLong;
        }

        Title = title.Trim();
        Description = description?.Trim();
        DueDate = dueDate;
        UpdatedAt = timeProvider.GetUtcNow();

        return this;
    }

    public Result<Todo> Complete(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (Status == TodoStatus.Done)
        {
            return TodoErrors.AlreadyCompleted;
        }

        Status = TodoStatus.Done;
        UpdatedAt = timeProvider.GetUtcNow();

        return this;
    }
}

public enum TodoStatus { Open, InProgress, Done }
