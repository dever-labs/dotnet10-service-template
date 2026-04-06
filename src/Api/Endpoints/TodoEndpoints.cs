using ServiceTemplate.Application.Common.Cqrs;
using Microsoft.AspNetCore.Mvc;
using ServiceTemplate.Api.Extensions;
using ServiceTemplate.Application.Todos;
using ServiceTemplate.Application.Todos.Commands.CompleteTodo;
using ServiceTemplate.Application.Todos.Commands.CreateTodo;
using ServiceTemplate.Application.Todos.Commands.DeleteTodo;
using ServiceTemplate.Application.Todos.Commands.UpdateTodo;
using ServiceTemplate.Application.Todos.Queries.GetTodo;
using ServiceTemplate.Application.Todos.Queries.GetTodos;
using ServiceTemplate.Domain.Todos;

namespace ServiceTemplate.Api.Endpoints;

internal static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos");

        group.MapGet("/", GetTodosAsync)
            .WithName("GetTodos")
            .WithSummary("Get a paged list of todos");

        group.MapGet("/{id:guid}", GetTodoAsync)
            .WithName("GetTodo")
            .WithSummary("Get a todo by ID")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTodoAsync)
            .WithName("CreateTodo")
            .WithSummary("Create a new todo")
            .Produces<TodoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", UpdateTodoAsync)
            .WithName("UpdateTodo")
            .WithSummary("Update an existing todo")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapPatch("/{id:guid}/complete", CompleteTodoAsync)
            .WithName("CompleteTodo")
            .WithSummary("Mark a todo as completed")
            .Produces<TodoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteTodoAsync)
            .WithName("DeleteTodo")
            .WithSummary("Delete a todo")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetTodosAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TodoStatus? status = null,
        ISender? sender = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender!.SendAsync(new GetTodosQuery(page, pageSize, status), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTodoAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new GetTodoQuery(id), cancellationToken);

        return result.Match(
            todo => Results.Ok(todo),
            error => error.ToProblemResult());
    }

    private static async Task<IResult> CreateTodoAsync(
        CreateTodoCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(command, cancellationToken);

        return result.Match(
            todo => Results.CreatedAtRoute("GetTodo", new { id = todo.Id }, todo),
            error => error.ToProblemResult());
    }

    private static async Task<IResult> UpdateTodoAsync(
        Guid id,
        [FromBody] UpdateTodoRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(
            new UpdateTodoCommand(id, request.Title, request.Description, request.DueDate),
            cancellationToken);

        return result.Match(
            todo => Results.Ok(todo),
            error => error.ToProblemResult());
    }

    private static async Task<IResult> CompleteTodoAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new CompleteTodoCommand(id), cancellationToken);

        return result.Match(
            todo => Results.Ok(todo),
            error => error.ToProblemResult());
    }

    private static async Task<IResult> DeleteTodoAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new DeleteTodoCommand(id), cancellationToken);

        return result.Match(
            _ => Results.NoContent(),
            error => error.ToProblemResult());
    }
}

#pragma warning disable CA1812 // Instantiated by ASP.NET Core model binding at runtime
internal sealed record UpdateTodoRequest(string Title, string? Description, DateTimeOffset? DueDate);
#pragma warning restore CA1812
