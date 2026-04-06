namespace ServiceTemplate.Fake.Endpoints;

internal static class FakeControlEndpoints
{
    public static IEndpointRouteBuilder MapFakeControlEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fake").WithTags("fake-control");

        // Seed todos directly — useful for setting up test state without going through the API
        // POST /fake/todos
        group.MapPost("/todos", (SeedTodoRequest req, FakeStore store) =>
        {
            var todo = store.Add(req.Title, req.Description, req.DueDate);
            return Results.Ok(TodoResponse.From(todo));
        });

        // Return all requests received since last reset.
        // Use this to assert that your service called the fake with the right data.
        // GET /fake/requests
        group.MapGet("/requests", (FakeStore store) =>
            Results.Ok(store.GetRecordedRequests()));

        // Clear all todos and recorded requests.
        // Call between test scenarios to ensure a clean slate.
        // POST /fake/reset
        group.MapPost("/reset", (FakeStore store) =>
        {
            store.Reset();
            return Results.NoContent();
        });

        return app;
    }
}

internal sealed record SeedTodoRequest(
    string Title,
    string? Description = null,
    DateTimeOffset? DueDate = null);
