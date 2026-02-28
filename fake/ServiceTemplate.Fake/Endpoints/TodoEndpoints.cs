using Microsoft.AspNetCore.Mvc;

namespace ServiceTemplate.Fake.Endpoints;

internal static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos").WithTags("Todos");

        group.MapGet("/", GetTodosAsync)
            .WithName("GetTodos");

        group.MapGet("/{id:guid}", GetTodoAsync)
            .WithName("GetTodo")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTodoAsync)
            .WithName("CreateTodo")
            .Produces<TodoResponse>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateTodoAsync)
            .WithName("UpdateTodo")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteTodoAsync)
            .WithName("DeleteTodo")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult GetTodosAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        FakeStore store,
        HttpContext ctx)
    {
        store.Record(new RecordedRequest("GET", ctx.Request.Path, null, DateTimeOffset.UtcNow));

        var all = store.GetAll();
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TodoResponse.From)
            .ToList();

        return Results.Ok(new PagedResult<TodoResponse>(items, all.Count, page, pageSize));
    }

    private static IResult GetTodoAsync(Guid id, FakeStore store, HttpContext ctx)
    {
        store.Record(new RecordedRequest("GET", ctx.Request.Path, null, DateTimeOffset.UtcNow));

        var todo = store.Get(id);
        return todo is null
            ? Results.Problem(detail: $"Todo {id} not found", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(TodoResponse.From(todo));
    }

    private static async Task<IResult> CreateTodoAsync(
        CreateTodoRequest request,
        FakeStore store,
        HttpContext ctx)
    {
        store.Record(new RecordedRequest("POST", ctx.Request.Path,
            await ReadBodyAsync(ctx), DateTimeOffset.UtcNow));

        var todo = store.Add(request.Title, request.Description, request.DueDate);
        return Results.CreatedAtRoute("GetTodo", new { id = todo.Id }, TodoResponse.From(todo));
    }

    private static async Task<IResult> UpdateTodoAsync(
        Guid id,
        [FromBody] UpdateTodoRequest request,
        FakeStore store,
        HttpContext ctx)
    {
        store.Record(new RecordedRequest("PUT", ctx.Request.Path,
            await ReadBodyAsync(ctx), DateTimeOffset.UtcNow));

        var todo = store.Update(id, request.Title, request.Description, request.DueDate);
        return todo is null
            ? Results.Problem(detail: $"Todo {id} not found", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(TodoResponse.From(todo));
    }

    private static IResult DeleteTodoAsync(Guid id, FakeStore store, HttpContext ctx)
    {
        store.Record(new RecordedRequest("DELETE", ctx.Request.Path, null, DateTimeOffset.UtcNow));

        return store.Delete(id)
            ? Results.NoContent()
            : Results.Problem(detail: $"Todo {id} not found", statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<string?> ReadBodyAsync(HttpContext ctx)
    {
        ctx.Request.EnableBuffering();
        using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        ctx.Request.Body.Position = 0;
        return body;
    }
}

// Mirror the real contract shapes — no dependency on Application project
internal sealed record TodoResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TodoResponse From(FakeTodo t) =>
        new(t.Id, t.Title, t.Description, t.Status, t.DueDate, t.CreatedAt, t.UpdatedAt);
}

internal sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

internal sealed record CreateTodoRequest(string Title, string? Description, DateTimeOffset? DueDate);
internal sealed record UpdateTodoRequest(string Title, string? Description, DateTimeOffset? DueDate);
