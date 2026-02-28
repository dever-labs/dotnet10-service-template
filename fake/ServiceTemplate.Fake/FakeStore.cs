using System.Collections.Concurrent;

namespace ServiceTemplate.Fake.Endpoints;

/// <summary>
/// In-memory store for the fake service.
/// Holds todos and a log of every request received.
/// Thread-safe — safe for concurrent test calls.
/// </summary>
public sealed class FakeStore
{
    private readonly ConcurrentDictionary<Guid, FakeTodo> _todos = new();
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    // ── Todos ─────────────────────────────────────────────────────────────────

    public FakeTodo Add(string title, string? description, DateTimeOffset? dueDate)
    {
        var todo = new FakeTodo(Guid.NewGuid(), title, description, "Pending", dueDate,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _todos[todo.Id] = todo;
        return todo;
    }

    public FakeTodo? Get(Guid id) =>
        _todos.GetValueOrDefault(id);

    public IReadOnlyList<FakeTodo> GetAll() =>
        [.. _todos.Values.OrderBy(t => t.CreatedAt)];

    public FakeTodo? Update(Guid id, string title, string? description, DateTimeOffset? dueDate)
    {
        if (!_todos.TryGetValue(id, out var existing)) return null;
        var updated = existing with
        {
            Title = title,
            Description = description,
            DueDate = dueDate,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _todos[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) =>
        _todos.TryRemove(id, out _);

    // ── Request log ───────────────────────────────────────────────────────────

    public void Record(RecordedRequest request) =>
        _requests.Enqueue(request);

    public IReadOnlyList<RecordedRequest> GetRecordedRequests() =>
        _requests.ToArray();

    // ── Reset ─────────────────────────────────────────────────────────────────

    public void Reset()
    {
        _todos.Clear();
        while (_requests.TryDequeue(out _)) { }
    }
}

public sealed record FakeTodo(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RecordedRequest(
    string Method,
    string Path,
    string? Body,
    DateTimeOffset ReceivedAt);
